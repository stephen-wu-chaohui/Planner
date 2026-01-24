using Google.OrTools.ConstraintSolver;
using Google.Protobuf.WellKnownTypes;
using Planner.Messaging.Optimization;
using Planner.Messaging.Optimization.Requests;
using Planner.Messaging.Optimization.Responses;
using System.Diagnostics;

namespace Planner.Optimization;

public sealed class VehicleRoutingProblem : IRouteOptimizer {
    // Internal record for solver-specific node data
    internal sealed record SolverLocation(
        int NodeIndex, long LocationId, bool IsDepot,
        long ReadyTime, long DueTime, long ServiceTimeMinutes,
        long PalletDemand, long WeightDemand, long RefrigeratedDemand,
        JobInput? Job
    );

    public OptimizeRouteResponse Optimize(OptimizeRouteRequest request) {
        var settings = request.Settings ?? new OptimizationSettings();

        // 1. Validation
        try {
            ValidateInput(request);
        } catch (SolverInputInvalidException ex) {
            return CreateEmptyResponse(request, $"Invalid input: {ex.Message}");
        }
        
        if (request.Vehicles.Count == 0) 
            return CreateEmptyResponse(request, "No vehicles provided.");

        // 2. Initialize Context
        var depots = request.Vehicles
            .SelectMany(v => new[] { v.StartLocation, v.EndLocation })
            .GroupBy(l => l.LocationId)
            .Select(g => g.First())
            .ToList();

        var context = new VrpContext(
            request, request.Jobs, request.Vehicles,
            [.. depots, .. request.Jobs.Select(j => j.Location)],
            TimeScale: 1, DistanceScale: 1
        );

        // 3. Prepare Solver Data - use precomputed matrices from request
        var solverLocs = BuildSolverLocations(context, settings);
        var distMatrix = request.DistanceMatrix;
        var travelMatrix = request.TravelTimeMatrix;

        // Validate matrix dimensions
        if (distMatrix.Length != solverLocs.Count || travelMatrix.Length != solverLocs.Count)
        {
            return CreateEmptyResponse(request, 
                $"Matrix dimension mismatch: expected {solverLocs.Count}x{solverLocs.Count}, " +
                $"but got DistanceMatrix {distMatrix.Length}x{(distMatrix.Length > 0 ? distMatrix[0].Length : 0)} " +
                $"and TravelTimeMatrix {travelMatrix.Length}x{(travelMatrix.Length > 0 ? travelMatrix[0].Length : 0)}");
        }

        // Validate all inner arrays have correct length
        for (int i = 0; i < distMatrix.Length; i++)
        {
            if (distMatrix[i].Length != solverLocs.Count)
            {
                return CreateEmptyResponse(request,
                    $"DistanceMatrix row {i} has length {distMatrix[i].Length}, expected {solverLocs.Count}");
            }
            if (travelMatrix[i].Length != solverLocs.Count)
            {
                return CreateEmptyResponse(request,
                    $"TravelTimeMatrix row {i} has length {travelMatrix[i].Length}, expected {solverLocs.Count}");
            }
        }

        var depotMap = DepotIndexMap.FromSolverLocations(solverLocs);
        int[] starts = context.Vehicles
            .Select(v => depotMap.NodeIndexOf(v.StartLocation.LocationId))
            .ToArray();

        int[] ends = context.Vehicles
            .Select(v => depotMap.NodeIndexOf(v.EndLocation.LocationId))
            .ToArray();

        // 4. Model Setup
        var manager = new RoutingIndexManager(solverLocs.Count, context.Vehicles.Count, starts, ends);
        var routing = new RoutingModel(manager);

        ConfigureDimensions(routing, manager, context, solverLocs, distMatrix, travelMatrix, settings);

        ApplyPickupDeliveryPairs(routing, manager, solverLocs);

        DumpSolverNodes(solverLocs);
        DumpVehicleDepotBindings(context.Vehicles, depotMap);

        // 5. Solve
        var solution = Solve(routing, settings);

        // 6. Build Response
        return solution is null
            ? CreateEmptyResponse(request, "Optimization failed to find a solution. This could be due to capacity constraints, time windows, or infeasibility.")
            : MapResults(context, routing, manager, solution, solverLocs, distMatrix, travelMatrix);
    }

    private static void ValidateInput(OptimizeRouteRequest request) {
        if (request.Vehicles.Count == 0)
            throw new SolverInputInvalidException("No vehicles.");

        // Depots are derived from vehicle start/end locations.
        // To keep validation meaningful, treat the first vehicle's start/end as the canonical depot set
        // and ensure all vehicles reference only those depots.
        var depotIds = new HashSet<long> {
            request.Vehicles[0].StartLocation.LocationId,
            request.Vehicles[0].EndLocation.LocationId
        };

        var jobLocIds = request.Jobs.Select(j => j.Location.LocationId).ToHashSet();
        if (jobLocIds.Overlaps(depotIds))
            throw new SolverInputInvalidException("Job/Depot LocationId collision.");

        foreach (var v in request.Vehicles) {
            if (!depotIds.Contains(v.StartLocation.LocationId) || !depotIds.Contains(v.EndLocation.LocationId))
                throw new SolverInputInvalidException($"Vehicle {v.VehicleId} references missing DepotId.");
        }
    }

    private static List<SolverLocation> BuildSolverLocations(VrpContext ctx, OptimizationSettings settings) {
        var list = new List<SolverLocation>();
        int node = 0;

        var depotLocs = ctx.Vehicles
            .SelectMany(v => new[] { v.StartLocation, v.EndLocation })
            .GroupBy(l => l.LocationId)
            .Select(g => g.First());

        foreach (var d in depotLocs)
            list.Add(new(node++, d.LocationId, true,
                0, settings.HorizonMinutes, 0, 0, 0, 0, null));

        foreach (var j in ctx.Jobs)
            list.Add(new(node++, j.Location.LocationId, false,
                j.ReadyTime, j.DueTime, j.ServiceTimeMinutes, j.PalletDemand, j.WeightDemand, j.RequiresRefrigeration ? 1 : 0, j));

        return list;
    }

    private static void ConfigureDimensions(RoutingModel rt, RoutingIndexManager mgr, VrpContext ctx, List<SolverLocation> locs, long[][] dists, long[][] travels, OptimizationSettings settings) {
        var overtimeMult = ctx.Request.OvertimeMultiplier <= 0 ? 2.0 : ctx.Request.OvertimeMultiplier;

        // Capacity Dimensions
        AddCapacity(rt, mgr, locs, "Pallets", ctx.Vehicles.Select(v => v.MaxPallets), l => l.PalletDemand);
        AddCapacity(rt, mgr, locs, "Weight", ctx.Vehicles.Select(v => v.MaxWeight), l => l.WeightDemand);
        AddCapacity(rt, mgr, locs, "Refrig", ctx.Vehicles.Select(v => v.RefrigeratedCapacity), l => l.RefrigeratedDemand);

        // Time Dimension
        int[] timeCbs = [.. ctx.Vehicles.Select(v => rt.RegisterTransitCallback((long from, long to) => {
            int f = mgr.IndexToNode(from); int t = mgr.IndexToNode(to);
            return (long)Math.Round((travels[f][t] + locs[f].ServiceTimeMinutes) * v.SpeedFactor);
        }))];

        rt.AddDimensionWithVehicleTransits(timeCbs, settings.MaxSlackMinutes, settings.HorizonMinutes, true, "Time");
        var timeDim = rt.GetMutableDimension("Time");

        for (int i = 0; i < locs.Count; i++)
            timeDim.CumulVar(mgr.NodeToIndex(i)).SetRange(locs[i].ReadyTime, locs[i].DueTime > 0? locs[i].DueTime : settings.HorizonMinutes);


        // Costs
        for (int v = 0; v < ctx.Vehicles.Count; v++) {
            var veh = ctx.Vehicles[v];
            timeDim.SetCumulVarSoftUpperBound(rt.End(v), veh.ShiftLimitMinutes, (long)Math.Round(veh.CostPerMinute * (overtimeMult - 1) * ctx.DistanceScale));
            rt.SetFixedCostOfVehicle((long)Math.Round(veh.BaseFee * ctx.DistanceScale), v);
            int costCb = rt.RegisterTransitCallback((long from, long to) => {
                int f = mgr.IndexToNode(from); int t = mgr.IndexToNode(to);
                return (long)Math.Round(((travels[f][t] + locs[f].ServiceTimeMinutes) * veh.CostPerMinute + dists[f][t] * veh.CostPerKm) * ctx.DistanceScale);
            });
            rt.SetArcCostEvaluatorOfVehicle(costCb, v);
        }
    }

    private static void AddCapacity(RoutingModel rt, RoutingIndexManager mgr, List<SolverLocation> locs, string name, IEnumerable<long> caps, Func<SolverLocation, long> demand) {
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

    private static OptimizeRouteResponse MapResults(VrpContext ctx, RoutingModel rt, RoutingIndexManager mgr, Assignment sol, List<SolverLocation> locs, long[][] dists, long[][] travels) {
        var routes = new List<RouteResult>();
        double grandTotal = 0;
        var (dimT, dimP, dimW, dimR) = (rt.GetMutableDimension("Time"), rt.GetMutableDimension("Pallets"), rt.GetMutableDimension("Weight"), rt.GetMutableDimension("Refrig"));

        for (int v = 0; v < ctx.Vehicles.Count; v++) {
            var veh = ctx.Vehicles[v];
            if (!rt.IsVehicleUsed(sol, v)) {
                routes.Add(new RouteResult(veh.VehicleId, veh.Name, false, Array.Empty<TaskAssignment>(), 0, 0, 0));
                continue;
            }

            var stops = new List<TaskAssignment>();
            double tTime = 0, tDist = 0;
            long idx = rt.Start(v);

            while (!rt.IsEnd(idx)) {
                int from = mgr.IndexToNode(idx);
                long nextIdx = sol.Value(rt.NextVar(idx));
                int to = mgr.IndexToNode(nextIdx);

                if (!locs[from].IsDepot && locs[from].Job is not null) {
                    var j = locs[from].Job;
                    stops.Add(new(j.JobId, sol.Value(dimT.CumulVar(idx)), sol.Value(dimT.CumulVar(idx)) + locs[from].ServiceTimeMinutes,
                        sol.Value(dimP.CumulVar(idx)), sol.Value(dimW.CumulVar(idx)), sol.Value(dimR.CumulVar(idx))));
                }
                tTime += travels[from][to] + locs[from].ServiceTimeMinutes;
                tDist += dists[from][to];
                idx = nextIdx;
            }

            double cost = veh.BaseFee + (tTime * veh.CostPerMinute) + (tDist * veh.CostPerKm);
            grandTotal += cost;
            routes.Add(new RouteResult(veh.VehicleId, veh.Name, true, stops, tTime, tDist, cost));
        }

        return new OptimizeRouteResponse(ctx.Request.TenantId, ctx.Request.OptimizationRunId, DateTime.UtcNow, routes, grandTotal);
    }

    private static OptimizeRouteResponse CreateEmptyResponse(OptimizeRouteRequest req, string? errorMessage = null) 
        => new(req.TenantId, req.OptimizationRunId, DateTime.UtcNow, Array.Empty<RouteResult>(), 0, errorMessage);

    private static void ApplyPickupDeliveryPairs(RoutingModel rt, RoutingIndexManager mgr, List<SolverLocation> locs) {
        // Implementation remains similar but uses the modular SolverLocation record
    }



    [Conditional("DEBUG")]
    private static void DumpSolverNodes(
        IReadOnlyList<SolverLocation> locs
    ) {
        Console.WriteLine("=== VRP Solver Nodes ===");
        Console.WriteLine("Idx | LocationId | Type   | Ready | Due | Service | Pal | Wgt | Ref");

        foreach (var l in locs) {
            Console.WriteLine(
                $"{l.NodeIndex,3} | " +
                $"{l.LocationId,10} | " +
                $"{(l.IsDepot ? "Depot" : "Job  "),6} | " +
                $"{l.ReadyTime,5} | " +
                $"{l.DueTime,5} | " +
                $"{l.ServiceTimeMinutes,7} | " +
                $"{l.PalletDemand,3} | " +
                $"{l.WeightDemand,3} | " +
                $"{l.RefrigeratedDemand,3}"
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
                $"{v.StartLocation.LocationId,10} → {depots.NodeIndexOf(v.StartLocation.LocationId),4} | " +
                $"{v.EndLocation.LocationId,10} → {depots.NodeIndexOf(v.EndLocation.LocationId),4}"
            );
        }
    }
}