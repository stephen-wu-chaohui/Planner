using Google.OrTools.ConstraintSolver;
using Google.Protobuf.WellKnownTypes;
using Planner.Contracts.Messages.VehicleRoutingProblem;
using Planner.Domain.Entities;

namespace Planner.Optimization.VehicleRoutingProblem;

public static class VrpSolver {
    public static VrpResult Solve(VrpRequest data) {
        const double overtimeMult = 2.0;
        const long slackMax = 60;

        int depotId = data.Jobs.FirstOrDefault(j => j.Type == JobType.Depot)?.Id
            ?? data.Jobs[0].Id;
        int vehicleCount = data.Vehicles.Count;

        var mgr = new RoutingIndexManager(data.Jobs.Count, vehicleCount, depotId);
        var rt = new RoutingModel(mgr);
        var slv = rt.solver();

        // ---------- TIME CALLBACK ----------
        int[] timeCb = new int[vehicleCount];
        for (int v = 0; v < vehicleCount; v++) {
            var veh = data.Vehicles[v];
            timeCb[v] = rt.RegisterTransitCallback((long from, long to) => {
                int i = mgr.IndexToNode(from);
                int j = mgr.IndexToNode(to);
                return (long)Math.Round((data.TravelMinutes[i][j] + data.Jobs[i].ServiceMinutes) * veh.SpeedFactor);
            });
        }

        // ---------- COST CALLBACK ----------
        int[] costCb = new int[vehicleCount];
        for (int v = 0; v < vehicleCount; v++) {
            var veh = data.Vehicles[v];
            double perMin = (veh.DriverRatePerHour + veh.MaintenanceRatePerHour) / 60.0;
            double perKm = veh.FuelRatePerKm;

            costCb[v] = rt.RegisterTransitCallback((long from, long to) => {
                int i = mgr.IndexToNode(from);
                int j = mgr.IndexToNode(to);
                double km = data.DistanceKm[i][j];
                double mins = data.TravelMinutes[i][j] + data.Jobs[i].ServiceMinutes;
                return (long)Math.Round(((mins * perMin) + (km * perKm)) * 1000);
            });
            rt.SetArcCostEvaluatorOfVehicle(costCb[v], v);
        }

        // ---------- CAPACITY DIMENSIONS ----------
        int palletCb = rt.RegisterUnaryTransitCallback(idx => data.Jobs[mgr.IndexToNode(idx)].PalletDemand);
        rt.AddDimensionWithVehicleCapacity(palletCb, 0, data.Vehicles.Select(v => v.MaxPallets).ToArray(), true, "Pallets");

        int weightCb = rt.RegisterUnaryTransitCallback(idx => data.Jobs[mgr.IndexToNode(idx)].WeightDemand);
        rt.AddDimensionWithVehicleCapacity(weightCb, 0, data.Vehicles.Select(v => v.MaxWeight).ToArray(), true, "Weight");

        int refrigCb = rt.RegisterUnaryTransitCallback(idx => data.Jobs[mgr.IndexToNode(idx)].RefrigeratedRequirement);
        rt.AddDimensionWithVehicleCapacity(refrigCb, 0, data.Vehicles.Select(v => v.RefrigeratedCapacity).ToArray(), true, "Refrig");

        // ---------- TIME DIMENSION ----------
        rt.AddDimensionWithVehicleTransits(timeCb, slackMax, 720, true, "Time");
        var dimTime = rt.GetMutableDimension("Time");

        foreach (var job in data.Jobs) {
            long idx = mgr.NodeToIndex(job.Id);
            dimTime.CumulVar(idx).SetRange(job.ReadyTime, job.DueTime);
        }

        // overtime + base fee
        for (int v = 0; v < vehicleCount; v++) {
            var veh = data.Vehicles[v];
            double perMin = (veh.DriverRatePerHour + veh.MaintenanceRatePerHour) / 60.0;
            dimTime.SetCumulVarSoftUpperBound(rt.End(v), veh.ShiftLimitMinutes,
                (long)Math.Round(perMin * (overtimeMult - 1) * 1000));
            rt.SetFixedCostOfVehicle((long)Math.Round(veh.BaseFee * 1000), v);
        }

        // ---------- PICKUP-DELIVERY PAIRS ----------
        int[,] pairs = { { 1, 2 }, { 3, 4 } }; // Demo; should be inferred later
        for (int i = 0; i < pairs.GetLength(0); i++) {
            long pu = mgr.NodeToIndex(pairs[i, 0]);
            long dl = mgr.NodeToIndex(pairs[i, 1]);
            rt.AddPickupAndDelivery(pu, dl);
            slv.Add(rt.VehicleVar(pu) == rt.VehicleVar(dl));
            slv.Add(dimTime.CumulVar(pu) <= dimTime.CumulVar(dl));
        }

        // ---------- SEARCH PARAMETERS ----------
        var search = operations_research_constraint_solver.DefaultRoutingSearchParameters();
        search.FirstSolutionStrategy = FirstSolutionStrategy.Types.Value.ParallelCheapestInsertion;
        search.LocalSearchMetaheuristic = LocalSearchMetaheuristic.Types.Value.GuidedLocalSearch;
        search.TimeLimit = new Duration { Seconds = 30 };
        search.LogSearch = false;

        var sol = rt.SolveWithParameters(search);
        if (sol == null) return new VrpResult();

        // ---------- BUILD RESULT ----------
        var result = new VrpResult();
        var dimPal = rt.GetMutableDimension("Pallets");
        var dimWgt = rt.GetMutableDimension("Weight");
        var dimRef = rt.GetMutableDimension("Refrig");

        for (int v = 0; v < vehicleCount; v++) {
            var veh = data.Vehicles[v];
            var route = new VehicleRoute {
                VehicleId = v,
                VehicleName = veh.Name,
                Used = rt.IsVehicleUsed(sol, v)
            };

            if (!route.Used) {
                // Empty route → no jobs
                result.Routes.Add(route);
                continue;
            }

            double perMin = (veh.DriverRatePerHour + veh.MaintenanceRatePerHour) / 60.0;
            double perKm = veh.FuelRatePerKm;
            double baseFee = veh.BaseFee;

            double totalTime = 0, totalKm = 0;

            long idx = rt.Start(v);
            while (!rt.IsEnd(idx)) {
                int fromNode = mgr.IndexToNode(idx);
                var job = data.Jobs[fromNode];
                long next = sol.Value(rt.NextVar(idx));
                int toNode = mgr.IndexToNode(next);

                double arr = sol.Value(dimTime.CumulVar(idx));
                double dep = arr + job.ServiceMinutes;
                long pal = sol.Value(dimPal.CumulVar(idx));
                long wgt = sol.Value(dimWgt.CumulVar(idx));
                long refg = sol.Value(dimRef.CumulVar(idx));

                // create RouteStop
                route.Stops.Add(new RouteStop(
                    job.Id,
                    job.Name,
                    job.Type,
                    arr,
                    dep,
                    pal,
                    wgt,
                    refg
                ));

                totalTime += data.TravelMinutes[fromNode][toNode] + job.ServiceMinutes;
                totalKm += data.DistanceKm[fromNode][toNode];
                idx = next;
            }

            route.TotalMinutes = totalTime;
            route.DistanceKm = totalKm;
            route.Cost = baseFee + totalTime * perMin + totalKm * perKm;
            result.TotalCost += route.Cost;
            result.Routes.Add(route);
        }

        return result;
    }
}