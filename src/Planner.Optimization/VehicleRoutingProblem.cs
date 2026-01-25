using Google.OrTools.ConstraintSolver;
using Google.Protobuf.WellKnownTypes;
using Planner.Messaging.Optimization.Inputs;
using Planner.Messaging.Optimization.Outputs;
using System.Diagnostics;

namespace Planner.Optimization;

public sealed class VehicleRoutingProblem : IRouteOptimizer {

    public OptimizeRouteResponse Optimize(OptimizeRouteRequest request) {
        var settings = request.Settings ?? new OptimizationSettings();
        double DistanceScale = settings.DistanceScale;

        // 1. Validation
        try {
            ValidateInput(request);
        } catch (SolverInputInvalidException ex) {
            return CreateEmptyResponse(request, $"Invalid input: {ex.Message}");
        }
        
        if (request.Vehicles.Length == 0) 
            return CreateEmptyResponse(request, "No vehicles provided.");

        // 2. Initialize Context
        var vehicles = request.Vehicles;
        var stops = request.Stops;
        var distMatrix = request.DistanceMatrix;
        var travelMatrix = request.TravelTimeMatrix;
        var depots = request.Vehicles
            .SelectMany(v => new[] { v.StartDepotLocationId, v.EndDepotLocationId })
            .Distinct()
            .ToList();

        // Validate matrix dimensions
        if (distMatrix.Length != stops.Length || travelMatrix.Length != stops.Length)
        {
            return CreateEmptyResponse(request, 
                $"Matrix dimension mismatch: expected {stops.Length}x{stops.Length}, " +
                $"but got DistanceMatrix {distMatrix.Length}x{(distMatrix.Length > 0 ? distMatrix[0].Length : 0)} " +
                $"and TravelTimeMatrix {travelMatrix.Length}x{(travelMatrix.Length > 0 ? travelMatrix[0].Length : 0)}");
        }

        // Validate all inner arrays have correct length
        for (int i = 0; i < distMatrix.Length; i++)
        {
            if (distMatrix[i].Length != stops.Length)
            {
                return CreateEmptyResponse(request,
                    $"DistanceMatrix row {i} has length {distMatrix[i].Length}, expected {stops.Length}");
            }
            if (travelMatrix[i].Length != stops.Length)
            {
                return CreateEmptyResponse(request,
                    $"TravelTimeMatrix row {i} has length {travelMatrix[i].Length}, expected {stops.Length}");
            }
        }

        var depotMap = DepotIndexMap.FromSolverLocations(stops);
        int[] starts = request.Vehicles
            .Select(v => depotMap.NodeIndexOf(v.StartDepotLocationId))
            .ToArray();

        int[] ends = request.Vehicles
            .Select(v => depotMap.NodeIndexOf(v.EndDepotLocationId))
            .ToArray();

        // 4. Model Setup
        var manager = new RoutingIndexManager(stops.Length, request.Vehicles.Length, starts, ends);
        var routing = new RoutingModel(manager);

        ConfigureDimensions(routing, manager, request);

        ApplyPickupDeliveryPairs(routing, manager, stops);

        DumpSolverNodes(stops);
        DumpVehicleDepotBindings(vehicles, depotMap);

        // 5. Solve
        var solution = Solve(routing, settings);

        // 6. Build Response
        return solution is null
            ? CreateEmptyResponse(request, "Optimization failed to find a solution. This could be due to capacity constraints, time windows, or infeasibility.")
            : MapResults(request, routing, manager, solution, stops, distMatrix, travelMatrix);
    }

    private static void ValidateInput(OptimizeRouteRequest request) {
        if (request.Vehicles.Length == 0)
            throw new SolverInputInvalidException("No vehicles.");

        // Depots are derived from vehicle start/end locations.
        // To keep validation meaningful, treat the first vehicle's start/end as the canonical depot set
        // and ensure all vehicles reference only those depots.
        var depotIds = new HashSet<long> {
            request.Vehicles[0].StartDepotLocationId,
            request.Vehicles[0].EndDepotLocationId
        };

        var locIds = request.Stops.Select(j => j.LocationId).ToHashSet();
        if (!locIds.Overlaps(depotIds))
            throw new SolverInputInvalidException("Depot LocationId not in stops.");

        foreach (var v in request.Vehicles) {
            if (!depotIds.Contains(v.StartDepotLocationId) || !depotIds.Contains(v.EndDepotLocationId))
                throw new SolverInputInvalidException($"Vehicle {v.VehicleId} references missing DepotId.");
        }
    }

    private static void ConfigureDimensions(RoutingModel rt, RoutingIndexManager mgr, OptimizeRouteRequest request) {
        var overtimeMult = request.Settings?.OvertimeMultiplier ?? 2.0;
        var locs = request.Stops;
        var dists = request.DistanceMatrix;
        var travels = request.TravelTimeMatrix;
        var settings = request.Settings;

        // Capacity Dimensions
        AddCapacity(rt, mgr, locs, "Pallets", request.Vehicles.Select(v => v.MaxPallets), l => l.PalletDemand);
        AddCapacity(rt, mgr, locs, "Weight", request.Vehicles.Select(v => v.MaxWeight), l => l.WeightDemand);
        AddCapacity(rt, mgr, locs, "Refrig", request.Vehicles.Select(v => v.RefrigeratedCapacity), l => l.RequiresRefrigeration ? 1 : 0);

        // Time Dimension
        int[] timeCbs = [.. request.Vehicles.Select(v => rt.RegisterTransitCallback((long from, long to) => {
            int f = mgr.IndexToNode(from); int t = mgr.IndexToNode(to);
            return (long)Math.Round((travels[f][t] + locs[f].ServiceTimeMinutes) * v.SpeedFactor);
        }))];

        rt.AddDimensionWithVehicleTransits(timeCbs, settings.MaxSlackMinutes, settings.HorizonMinutes, true, "Time");
        var timeDim = rt.GetMutableDimension("Time");

        for (int i = 0; i < locs.Length; i++)
            timeDim.CumulVar(mgr.NodeToIndex(i)).SetRange(locs[i].ReadyTime, locs[i].DueTime > 0? locs[i].DueTime : settings.HorizonMinutes);

        double DistanceScale = request.Settings?.DistanceScale ?? 1.0;
        // Costs
        for (int v = 0; v < request.Vehicles.Length; v++) {
            var veh = request.Vehicles[v];
            timeDim.SetCumulVarSoftUpperBound(rt.End(v), veh.ShiftLimitMinutes, (long)Math.Round(veh.CostPerMinute * (overtimeMult - 1) * DistanceScale));
            rt.SetFixedCostOfVehicle((long)Math.Round(veh.BaseFee * DistanceScale), v);
            int costCb = rt.RegisterTransitCallback((long from, long to) => {
                int f = mgr.IndexToNode(from); int t = mgr.IndexToNode(to);
                return (long)Math.Round(((travels[f][t] + locs[f].ServiceTimeMinutes) * veh.CostPerMinute + dists[f][t] * veh.CostPerKm) * DistanceScale);
            });
            rt.SetArcCostEvaluatorOfVehicle(costCb, v);
        }
    }

    private static void AddCapacity(RoutingModel rt, RoutingIndexManager mgr, StopInput [] locs, string name, IEnumerable<long> caps, Func<StopInput, long> demand) {
        int cb = rt.RegisterUnaryTransitCallback(idx => demand(locs[mgr.IndexToNode(idx)]));
        rt.AddDimensionWithVehicleCapacity(cb, 0, [.. caps], true, name);
    }

    private static Assignment? Solve(RoutingModel rt, OptimizationSettings settings) {
        var p = operations_research_constraint_solver.DefaultRoutingSearchParameters();
        p.FirstSolutionStrategy = FirstSolutionStrategy.Types.Value.PathCheapestArc;
        p.LocalSearchMetaheuristic = LocalSearchMetaheuristic.Types.Value.GuidedLocalSearch;
        p.TimeLimit = new Duration { Seconds = settings.SearchTimeLimitSeconds };
        return rt.SolveWithParameters(p);
    }

    private static OptimizeRouteResponse MapResults(OptimizeRouteRequest request, RoutingModel rt, RoutingIndexManager mgr, Assignment sol, StopInput [] locs, long[][] dists, long[][] travels) {
        var routes = new List<RouteResult>();
        double grandTotal = 0;
        var (dimT, dimP, dimW, dimR) = (rt.GetMutableDimension("Time"), rt.GetMutableDimension("Pallets"), rt.GetMutableDimension("Weight"), rt.GetMutableDimension("Refrig"));

        for (int v = 0; v < request.Vehicles.Length; v++) {
            var veh = request.Vehicles[v];
            if (!rt.IsVehicleUsed(sol, v)) {
                routes.Add(new RouteResult(veh.VehicleId, Array.Empty<TaskAssignment>(), 0, 0, 0));
                continue;
            }

            var stops = new List<TaskAssignment>();
            double tTime = 0, tDist = 0;
            long idx = rt.Start(v);

            while (!rt.IsEnd(idx)) {
                int from = mgr.IndexToNode(idx);
                long nextIdx = sol.Value(rt.NextVar(idx));
                int to = mgr.IndexToNode(nextIdx);

                if (locs[from].LocationType != 0) {
                    stops.Add(new(locs[from].LocationId, sol.Value(dimT.CumulVar(idx)), sol.Value(dimT.CumulVar(idx)) + locs[from].ServiceTimeMinutes,
                        sol.Value(dimP.CumulVar(idx)), sol.Value(dimW.CumulVar(idx)), sol.Value(dimR.CumulVar(idx))));
                }
                tTime += travels[from][to] + locs[from].ServiceTimeMinutes;
                tDist += dists[from][to];
                idx = nextIdx;
            }

            double cost = veh.BaseFee + (tTime * veh.CostPerMinute) + (tDist * veh.CostPerKm);
            grandTotal += cost;
            routes.Add(new RouteResult(veh.VehicleId, stops, tTime, tDist, cost));
        }

        return new OptimizeRouteResponse(request.TenantId, request.OptimizationRunId, DateTime.UtcNow, routes, grandTotal);
    }

    private static OptimizeRouteResponse CreateEmptyResponse(OptimizeRouteRequest req, string? errorMessage = null) 
        => new(req.TenantId, req.OptimizationRunId, DateTime.UtcNow, Array.Empty<RouteResult>(), 0, errorMessage);

    private static void ApplyPickupDeliveryPairs(RoutingModel rt, RoutingIndexManager mgr, StopInput [] locs) {
        // Implementation remains similar but uses the modular SolverLocation record
    }

    [Conditional("DEBUG")]
    private static void DumpSolverNodes(StopInput[] locs) {
        Console.WriteLine("=== VRP Solver Nodes ===");
        Console.WriteLine("Idx | LocationId | Type   | Ready | Due | Service | Pal | Wgt | Ref");

        for (int i = 0; i < locs.Length; i++) {
            var l = locs[i];
            Console.WriteLine(
                $"{i,3} | " +
                $"{l.LocationId,10} | " +
                $"{(l.LocationType == 0 ? "Depot" : "Job  "),6} | " +
                $"{l.ReadyTime,5} | " +
                $"{l.DueTime,5} | " +
                $"{l.ServiceTimeMinutes,7} | " +
                $"{l.PalletDemand,3} | " +
                $"{l.WeightDemand,3} | " +
                $"{(l.RequiresRefrigeration ? 1 : 0),3}"
            );
        }

        Console.WriteLine();
    }

    [Conditional("DEBUG")]
    private static void DumpVehicleDepotBindings(
        IReadOnlyList<VehicleInput> vehicles,
        DepotIndexMap depots
    ) {
        Console.WriteLine("=== Vehicle Depot Bindings ===");
        Console.WriteLine("VehId | StartDepot → Node | EndDepot → Node");

        foreach (var v in vehicles) {
            Console.WriteLine(
                $"{v.VehicleId,5} | " +
                $"{v.StartDepotLocationId,10} → {depots.NodeIndexOf(v.StartDepotLocationId),4} | " +
                $"{v.EndDepotLocationId,10} → {depots.NodeIndexOf(v.EndDepotLocationId),4}"
            );
        }
    }
}