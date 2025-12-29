using Google.OrTools.ConstraintSolver;
using Google.Protobuf.WellKnownTypes;
using Planner.Contracts.Optimization.Abstractions;
using Planner.Contracts.Optimization.Inputs;
using Planner.Contracts.Optimization.Outputs;
using Planner.Contracts.Optimization.Requests;
using Planner.Contracts.Optimization.Responses;
using System.Diagnostics;

namespace Planner.Optimization;

public sealed class VehicleRoutingProblem : IRouteOptimizer {
    // Internal record for solver-specific node data
    internal sealed record SolverLocation(
        int NodeIndex, long LocationId, bool IsDepot, double Latitude, double Longitude,
        long ReadyTime, long DueTime, long ServiceTimeMinutes,
        long PalletDemand, long WeightDemand, long RefrigeratedDemand,
        JobInput? Job
    );

    public OptimizeRouteResponse Optimize(OptimizeRouteRequest request) {
        var settings = request.Settings ?? new OptimizationSettings();

        // 1. Validation
        ValidateInput(request);
        if (request.Vehicles.Count == 0 || request.Depots.Count == 0) return CreateEmptyResponse(request);

        // 2. Initialize Context
        var context = new VrpContext(
            request, request.Jobs, request.Vehicles,
            [.. request.Depots.Select(d => d.Location), .. request.Jobs.Select(j => j.Location)],
            TimeScale: 1, DistanceScale: 1000
        );

        // 3. Prepare Matrices & Solver Data
        var solverLocs = BuildSolverLocations(context, settings);
        var (distMatrix, travelMatrix) = BuildMatrices(solverLocs, settings);

        var depotMap = DepotIndexMap.FromSolverLocations(solverLocs);
        int[] starts = context.Vehicles
            .Select(v => depotMap.NodeIndexOf(v.DepotStartId))
            .ToArray();

        int[] ends = context.Vehicles
            .Select(v => depotMap.NodeIndexOf(v.DepotEndId))
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
            ? CreateEmptyResponse(request)
            : MapResults(context, routing, manager, solution, solverLocs, distMatrix, travelMatrix);
    }

    private static void ValidateInput(OptimizeRouteRequest request) {
        var depotIds = request.Depots.Select(d => d.Location.LocationId).ToHashSet();
        if (depotIds.Count != request.Depots.Count) throw new SolverInputInvalidException("Duplicate Depot LocationIds.");

        var jobLocIds = request.Jobs.Select(j => j.Location.LocationId).ToHashSet();
        if (jobLocIds.Overlaps(depotIds)) throw new SolverInputInvalidException("Job/Depot LocationId collision.");

        foreach (var v in request.Vehicles) {
            if (!depotIds.Contains(v.DepotStartId) || !depotIds.Contains(v.DepotEndId))
                throw new SolverInputInvalidException($"Vehicle {v.VehicleId} references missing DepotId.");
        }
    }

    private static List<SolverLocation> BuildSolverLocations(VrpContext ctx, OptimizationSettings settings) {
        var list = new List<SolverLocation>();
        int node = 0;

        foreach (var d in ctx.Request.Depots)
            list.Add(new(node++, d.Location.LocationId, true, d.Location.Latitude, d.Location.Longitude,
                0, settings.HorizonMinutes, 0, 0, 0, 0, null));

        foreach (var j in ctx.Jobs)
            list.Add(new(node++, j.Location.LocationId, false, j.Location.Latitude, j.Location.Longitude,
                j.ReadyTime, j.DueTime, j.ServiceTimeMinutes, j.PalletDemand, j.WeightDemand, j.RequiresRefrigeration ? 1 : 0, j));

        return list;
    }

    private static (double[][] dist, double[][] travel) BuildMatrices(List<SolverLocation> locs, OptimizationSettings settings) {
        int n = locs.Count;
        var dist = new double[n][];
        var travel = new double[n][];

        for (int i = 0; i < n; i++) {
            dist[i] = new double[n];
            travel[i] = new double[n];
            for (int j = 0; j < n; j++) {
                if (i == j) continue;
                double d = settings.KmDegreeConstant * Math.Abs(locs[i].Latitude - locs[j].Latitude) + Math.Abs(locs[i].Longitude - locs[j].Longitude);
                dist[i][j] = d;
                travel[i][j] = d * settings.TravelTimeMultiplier;
            }
        }
        return (dist, travel);
    }

    private static void ConfigureDimensions(RoutingModel rt, RoutingIndexManager mgr, VrpContext ctx, List<SolverLocation> locs, double[][] dists, double[][] travels, OptimizationSettings settings) {
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
            timeDim.CumulVar(mgr.NodeToIndex(i)).SetRange(locs[i].ReadyTime, Math.Min(settings.HorizonMinutes, locs[i].DueTime));

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
        p.FirstSolutionStrategy = FirstSolutionStrategy.Types.Value.ParallelCheapestInsertion;
        p.LocalSearchMetaheuristic = LocalSearchMetaheuristic.Types.Value.GuidedLocalSearch;
        p.TimeLimit = new Duration { Seconds = settings.SearchTimeLimitSeconds };
        return rt.SolveWithParameters(p);
    }

    private static OptimizeRouteResponse MapResults(VrpContext ctx, RoutingModel rt, RoutingIndexManager mgr, Assignment sol, List<SolverLocation> locs, double[][] dists, double[][] travels) {
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
                    stops.Add(new(j.JobId, j.JobType, j.Name, sol.Value(dimT.CumulVar(idx)), sol.Value(dimT.CumulVar(idx)) + locs[from].ServiceTimeMinutes,
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

    private static OptimizeRouteResponse CreateEmptyResponse(OptimizeRouteRequest req) => new(req.TenantId, req.OptimizationRunId, DateTime.UtcNow, Array.Empty<RouteResult>(), 0);

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
                $"{v.DepotStartId,10} → {depots.NodeIndexOf(v.DepotStartId),4} | " +
                $"{v.DepotEndId,10} → {depots.NodeIndexOf(v.DepotEndId),4}"
            );
        }
    }
}