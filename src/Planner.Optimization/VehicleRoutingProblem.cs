using Google.OrTools.ConstraintSolver;
using Google.Protobuf.WellKnownTypes;
using Planner.Contracts.Optimization.Abstractions;
using Planner.Contracts.Optimization.Inputs;
using Planner.Contracts.Optimization.Outputs;
using Planner.Contracts.Optimization.Requests;
using Planner.Contracts.Optimization.Responses;

namespace Planner.Optimization;

public sealed class VehicleRoutingProblem : IRouteOptimizer {
    // Solver-internal node model: contains everything callbacks need.
    internal sealed record SolverLocation(
        int NodeIndex,
        long LocationId,
        bool IsDepot,
        double Latitude,
        double Longitude,

        // Time window and service
        long ReadyTime,
        long DueTime,
        long ServiceTimeMinutes,

        // Demands
        long PalletDemand,
        long WeightDemand,
        long RefrigeratedDemand,

        // Back-reference (null for depots)
        JobInput? Job
    );

    public OptimizeRouteResponse Optimize(OptimizeRouteRequest request) {
        // Prefer request value if present (you had this in contracts).
        // If you remove it later, revert to constant.
        var overtimeMult = request.OvertimeMultiplier <= 0 ? 2.0 : request.OvertimeMultiplier;

        const long slackMax = 60;
        const long horizonMinutes = 720; // keep consistent with your current model

        var depots = request.Depots;
        var jobs = request.Jobs;
        var vehicles = request.Vehicles;

        // ---------- INPUT VALIDATION (uniqueness & referential integrity) ----------

        // Depots must be unique by LocationId
        var depotIds = depots.Select(d => d.Location.LocationId).ToArray();
        if (depotIds.Length != depotIds.Distinct().Count()) {
            var dup = depotIds.GroupBy(x => x).First(g => g.Count() > 1).Key;
            throw new SolverInputInvalidException($"Duplicate DepotInput.LocationId detected: {dup}");
        }

        // Jobs must be unique by LocationId (because we build a single node per LocationId)
        var jobLocationIds = jobs.Select(j => j.Location.LocationId).ToArray();
        if (jobLocationIds.Length != jobLocationIds.Distinct().Count()) {
            var dup = jobLocationIds.GroupBy(x => x).First(g => g.Count() > 1).Key;
            throw new SolverInputInvalidException($"Duplicate JobInput.LocationId detected: {dup}");
        }

        // Jobs must be unique by JobId (stable mapping and output correctness)
        var jobIds = jobs.Select(j => j.JobId).ToArray();
        if (jobIds.Length != jobIds.Distinct().Count()) {
            var dup = jobIds.GroupBy(x => x).First(g => g.Count() > 1).Key;
            throw new SolverInputInvalidException($"Duplicate JobInput.JobId detected: {dup}");
        }

        // Depots and Jobs must not collide on LocationId
        var depotIdSet = depotIds.ToHashSet();
        var collision = jobLocationIds.FirstOrDefault(id => depotIdSet.Contains(id));
        if (collision != 0 || depotIdSet.Contains(0) && jobLocationIds.Contains(0)) {
            // The 'collision != 0' check is not sufficient if collision value is 0, so we check both.
            // We just need a reliable way to detect "any collision" with long values.
            var collisions = jobLocationIds.Where(id => depotIdSet.Contains(id)).Distinct().ToArray();
            if (collisions.Length > 0)
                throw new SolverInputInvalidException($"Depot/Job LocationId collision detected. LocationId(s): {string.Join(", ", collisions)}");
        }

        // Vehicle depot references must exist
        for (int v = 0; v < vehicles.Count; v++) {
            var veh = vehicles[v];

            if (!depotIdSet.Contains(veh.DepotStartId))
                throw new SolverInputInvalidException($"Vehicle {veh.VehicleId} DepotStartId not found in depots: {veh.DepotStartId}");

            if (!depotIdSet.Contains(veh.DepotEndId))
                throw new SolverInputInvalidException($"Vehicle {veh.VehicleId} DepotEndId not found in depots: {veh.DepotEndId}");
        }

        int vehicleCount = vehicles.Count;


        if (vehicleCount == 0 || depots.Count == 0)
            return new OptimizeRouteResponse {
                TenantId = request.TenantId,
                OptimizationRunId = request.OptimizationRunId
            };

        // Build solver locations and O(1) LocationId -> NodeIndex map.
        var solverLocations = new List<SolverLocation>(depots.Count + jobs.Count);
        var nodeByLocationId = new Dictionary<long, int>(capacity: depots.Count + jobs.Count);

        int node = 0;

        // Depots first
        foreach (var d in depots) {
            solverLocations.Add(new SolverLocation(
                NodeIndex: node,
                IsDepot: true,
                LocationId: d.Location.LocationId,
                Latitude: d.Location.Latitude,
                Longitude: d.Location.Longitude,
                ReadyTime: 0,
                DueTime: horizonMinutes,      // depot open whole horizon (adjust if needed)
                ServiceTimeMinutes: 0,
                PalletDemand: 0,
                WeightDemand: 0,
                RefrigeratedDemand: 0,
                Job: null
            ));

            nodeByLocationId[d.Location.LocationId] = node;
            node++;
        }

        // Jobs next
        foreach (var j in jobs) {
            solverLocations.Add(new SolverLocation(
                NodeIndex: node,
                LocationId: j.Location.LocationId,
                IsDepot: false,
                Latitude: j.Location.Latitude,
                Longitude: j.Location.Longitude,
                ReadyTime: j.ReadyTime,
                DueTime: j.DueTime,
                ServiceTimeMinutes: j.ServiceTimeMinutes,
                PalletDemand: j.PalletDemand,
                WeightDemand: j.WeightDemand,
                RefrigeratedDemand: j.RequiresRefrigeration ? 1 : 0,
                Job: j
            ));

            nodeByLocationId[j.Location.LocationId] = node;
            node++;
        }

        int nodeCount = solverLocations.Count;

        // Distance matrix (km-ish) and travel minutes.
        const double kmDegree = 111.3200;

        var distanceKm = new double[nodeCount][];
        for (int i = 0; i < nodeCount; i++) {
            distanceKm[i] = new double[nodeCount];
            for (int j = 0; j < nodeCount; j++) {
                if (i == j) {
                    distanceKm[i][j] = 0;
                    continue;
                }

                distanceKm[i][j] =
                    kmDegree * Math.Abs(solverLocations[i].Latitude - solverLocations[j].Latitude)
                  + Math.Abs(solverLocations[i].Longitude - solverLocations[j].Longitude);
            }
        }

        var travelMinutes = distanceKm
            .Select(row => row.Select(x => x * 2).ToArray())
            .ToArray();

        // Per-vehicle start/end nodes (multi-depot).
        var starts = vehicles.Select(v => nodeByLocationId[v.DepotStartId]).ToArray();
        var ends = vehicles.Select(v => nodeByLocationId[v.DepotEndId]).ToArray();

        var mgr = new RoutingIndexManager(nodeCount, vehicleCount, starts, ends);
        var rt = new RoutingModel(mgr);
        var solver = rt.solver();

        // ---------- TIME CALLBACK ----------
        // Time = travel(from->to) + service(at from), scaled by vehicle speed factor.
        int[] timeCb = new int[vehicleCount];
        for (int v = 0; v < vehicleCount; v++) {
            var veh = vehicles[v];

            timeCb[v] = rt.RegisterTransitCallback((long from, long to) => {
                int fromNode = mgr.IndexToNode(from);
                int toNode = mgr.IndexToNode(to);

                var fromLoc = solverLocations[fromNode];

                double mins = travelMinutes[fromNode][toNode] + fromLoc.ServiceTimeMinutes;
                return (long)Math.Round(mins * veh.SpeedFactor);
            });
        }

        // ---------- COST CALLBACK ----------
        // TotalCost = mins * CostPerMinute + km * CostPerKm
        int[] costCb = new int[vehicleCount];
        for (int v = 0; v < vehicleCount; v++) {
            var veh = vehicles[v];
            double perMin = veh.CostPerMinute;
            double perKm = veh.CostPerKm;

            costCb[v] = rt.RegisterTransitCallback((long from, long to) => {
                int fromNode = mgr.IndexToNode(from);
                int toNode = mgr.IndexToNode(to);

                double km = distanceKm[fromNode][toNode];
                double mins = travelMinutes[fromNode][toNode] + solverLocations[fromNode].ServiceTimeMinutes;

                return (long)Math.Round(((mins * perMin) + (km * perKm)) * 1000);
            });

            rt.SetArcCostEvaluatorOfVehicle(costCb[v], v);
        }

        // ---------- CAPACITY DIMENSIONS ----------
        int palletCb = rt.RegisterUnaryTransitCallback(idx => {
            int nodeIdx = mgr.IndexToNode(idx);
            return solverLocations[nodeIdx].PalletDemand;
        });
        rt.AddDimensionWithVehicleCapacity(
            palletCb,
            0,
            [.. vehicles.Select(v => v.MaxPallets)],
            true,
            "Pallets"
        );

        int weightCb = rt.RegisterUnaryTransitCallback(idx => {
            int nodeIdx = mgr.IndexToNode(idx);
            return solverLocations[nodeIdx].WeightDemand;
        });
        rt.AddDimensionWithVehicleCapacity(
            weightCb,
            0,
            [.. vehicles.Select(v => v.MaxWeight)],
            true,
            "Weight"
        );

        int refrigCb = rt.RegisterUnaryTransitCallback(idx => {
            int nodeIdx = mgr.IndexToNode(idx);
            return solverLocations[nodeIdx].RefrigeratedDemand;
        });
        rt.AddDimensionWithVehicleCapacity(
            refrigCb,
            0,
            [.. vehicles.Select(v => v.RefrigeratedCapacity)],
            true,
            "Refrig"
        );

        // ---------- TIME DIMENSION ----------
        rt.AddDimensionWithVehicleTransits(timeCb, slackMax, horizonMinutes, true, "Time");
        var dimTime = rt.GetMutableDimension("Time");

        // Apply time windows to every node (depots included).
        for (int n = 0; n < nodeCount; n++) {
            var loc = solverLocations[n];
            long idx = mgr.NodeToIndex(n);

            // Ensure bounds are within horizon
            long ready = Math.Max(0, loc.ReadyTime);
            long due = Math.Min(horizonMinutes, loc.DueTime);

            if (due < ready) due = ready;

            dimTime.CumulVar(idx).SetRange(ready, due);
        }

        // overtime + base fee
        for (int v = 0; v < vehicleCount; v++) {
            var veh = vehicles[v];

            dimTime.SetCumulVarSoftUpperBound(
                rt.End(v),
                veh.ShiftLimitMinutes,
                (long)Math.Round(veh.CostPerMinute * (overtimeMult - 1) * 1000)
            );

            rt.SetFixedCostOfVehicle((long)Math.Round(veh.BaseFee * 1000), v);
        }

        // ---------- PICKUP-DELIVERY PAIRS ----------
        // Demo only. If you keep this, map by JobId to node index safely.
        int[,] pairs = { { 1, 2 }, { 3, 4 } }; // Demo: JobId values, NOT node indices.

        // Build JobId -> node index map once (no per-loop searches).
        var nodeByJobId = new Dictionary<int, int>();
        foreach (var loc in solverLocations) {
            if (loc.Job is null) continue;
            nodeByJobId[loc.Job.JobId] = loc.NodeIndex;
        }

        for (int i = 0; i < pairs.GetLength(0); i++) {
            int puJobId = pairs[i, 0];
            int dlJobId = pairs[i, 1];

            if (!nodeByJobId.TryGetValue(puJobId, out var puNode) ||
                !nodeByJobId.TryGetValue(dlJobId, out var dlNode))
                continue;

            long pu = mgr.NodeToIndex(puNode);
            long dl = mgr.NodeToIndex(dlNode);

            rt.AddPickupAndDelivery(pu, dl);
            solver.Add(rt.VehicleVar(pu) == rt.VehicleVar(dl));
            solver.Add(dimTime.CumulVar(pu) <= dimTime.CumulVar(dl));
        }

        // ---------- SEARCH PARAMETERS ----------
        var search = operations_research_constraint_solver.DefaultRoutingSearchParameters();
        search.FirstSolutionStrategy = FirstSolutionStrategy.Types.Value.ParallelCheapestInsertion;
        search.LocalSearchMetaheuristic = LocalSearchMetaheuristic.Types.Value.GuidedLocalSearch;
        search.TimeLimit = new Duration { Seconds = 30 };
        search.LogSearch = false;

        var sol = rt.SolveWithParameters(search);

        if (sol is null)
            return new OptimizeRouteResponse {
                TenantId = request.TenantId,
                OptimizationRunId = request.OptimizationRunId
            };

        // ---------- BUILD RESULT ----------
        var routes = new List<RouteResult>(capacity: vehicleCount);
        double totalCostAll = 0;

        var dimPal = rt.GetMutableDimension("Pallets");
        var dimWgt = rt.GetMutableDimension("Weight");
        var dimRef = rt.GetMutableDimension("Refrig");

        for (int v = 0; v < vehicleCount; v++) {
            var veh = vehicles[v];

            bool used = rt.IsVehicleUsed(sol, v);

            if (!used) {
                routes.Add(new RouteResult {
                    VehicleId = veh.VehicleId,
                    VehicleName = veh.Name,
                    Used = false,
                    Stops = Array.Empty<TaskAssignment>(),
                    TotalMinutes = 0,
                    TotalDistanceKm = 0,
                    TotalCost = 0
                });

                continue;
            }

            double perMin = veh.CostPerMinute;
            double perKm = veh.CostPerKm;
            double baseFee = veh.BaseFee;

            double totalTime = 0;
            double totalKm = 0;

            var routeStops = new List<TaskAssignment>();

            long idx = rt.Start(v);

            while (!rt.IsEnd(idx)) {
                int fromNode = mgr.IndexToNode(idx);
                var fromLoc = solverLocations[fromNode];

                long next = sol.Value(rt.NextVar(idx));
                int toNode = mgr.IndexToNode(next);

                double arrivalTime = sol.Value(dimTime.CumulVar(idx));
                double departureTime = arrivalTime + fromLoc.ServiceTimeMinutes;

                long palletLoad = sol.Value(dimPal.CumulVar(idx));
                long weightLoad = sol.Value(dimWgt.CumulVar(idx));
                long refrigeratedLoad = sol.Value(dimRef.CumulVar(idx));

                // Emit stops only for real jobs (depots are not stops in output).
                if (!fromLoc.IsDepot && fromLoc.Job is not null) {
                    var job = fromLoc.Job;

                    routeStops.Add(new TaskAssignment(
                        job.JobId,
                        job.JobType,
                        job.Name,
                        arrivalTime,
                        departureTime,
                        palletLoad,
                        weightLoad,
                        refrigeratedLoad
                    ));
                }

                // Accumulate costs/time/distance on arcs
                totalTime += travelMinutes[fromNode][toNode] + fromLoc.ServiceTimeMinutes;
                totalKm += distanceKm[fromNode][toNode];

                idx = next;
            }

            double routeCost = baseFee + (totalTime * perMin) + (totalKm * perKm);
            totalCostAll += routeCost;

            routes.Add(new RouteResult {
                VehicleId = veh.VehicleId,
                VehicleName = veh.Name,
                Used = true,
                Stops = routeStops,
                TotalMinutes = totalTime,
                TotalDistanceKm = totalKm,
                TotalCost = routeCost
            });
        }

        return new OptimizeRouteResponse {
            TenantId = request.TenantId,
            OptimizationRunId = request.OptimizationRunId,
            CompletedAt = DateTime.UtcNow,
            Routes = routes,
            TotalCost = totalCostAll
        };
    }
}
