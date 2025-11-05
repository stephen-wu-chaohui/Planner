using System;
using System.Collections.Generic;
using System.Text;
using Google.OrTools.ConstraintSolver;
using Google.Protobuf.WellKnownTypes;

#region --- Core domain models ---

public enum JobType { Depot, Pickup, Delivery }

public class Job {
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public JobType Type { get; set; } = JobType.Pickup;
    public long ReadyTime { get; set; }
    public long DueTime { get; set; }
    public double ServiceMinutes { get; set; }
    public long PalletDemand { get; set; }
    public long WeightDemand { get; set; }
    public long RefrigeratedRequirement { get; set; }
}

public class Vehicle {
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public double SpeedFactor { get; set; } = 1.0;
    public long ShiftLimitMinutes { get; set; } = 480;
    public long DepotStartId { get; set; } = 0;
    public long DepotEndId { get; set; } = 0;
    public double DriverRatePerHour { get; set; }
    public double MaintenanceRatePerHour { get; set; }
    public double FuelRatePerKm { get; set; }
    public double BaseFee { get; set; }
    public long MaxPallets { get; set; }
    public long MaxWeight { get; set; }
    public long RefrigeratedCapacity { get; set; } = 0;
}

public class VrpInput {
    public List<Job> Jobs { get; set; } = new();
    public List<Vehicle> Vehicles { get; set; } = new();
    public double[][] DistanceKm { get; set; } = default!;
    public double[][] TravelMinutes { get; set; } = default!;
}

public class VehicleRoute {
    public int VehicleId { get; set; }
    public string VehicleName { get; set; } = string.Empty;
    public bool Used { get; set; }
    public List<string> Stops { get; set; } = new();
    public double TotalMinutes { get; set; }
    public double DistanceKm { get; set; }
    public double Cost { get; set; }
}

public class VrpResult {
    public List<VehicleRoute> Routes { get; set; } = new();
    public double TotalCost { get; set; }
}

#endregion

class Program {
    static void Main() {
        Console.OutputEncoding = Encoding.UTF8;
        var input = BuildSampleInput();
        var result = SolveVrp(input);
        PrintResult(result);
        Console.ReadKey();
    }

    // ------------------- Build sample input -------------------
    static VrpInput BuildSampleInput() {
        return new VrpInput {
            DistanceKm = new double[][] {
                new double[]{0,10,18,22,28},
                new double[]{10,0,8,16,24},
                new double[]{18,8,0,12,20},
                new double[]{22,16,12,0,10},
                new double[]{28,24,20,10,0}
            },
            TravelMinutes = new double[][] {
                new double[]{0,15,25,30,40},
                new double[]{15,0,10,20,30},
                new double[]{25,10,0,15,25},
                new double[]{30,20,15,0,12},
                new double[]{40,30,25,12,0}
            },
            Jobs = new List<Job>
            {
                new Job{Id=0,Name="Depot",Type=JobType.Depot,ReadyTime=0,DueTime=720,ServiceMinutes=0},
                new Job{Id=1,Name="Pickup A",Type=JobType.Pickup,ReadyTime=30,DueTime=480,ServiceMinutes=10,PalletDemand=+10,WeightDemand=+700,RefrigeratedRequirement=1},
                new Job{Id=2,Name="Delivery A",Type=JobType.Delivery,ReadyTime=120,DueTime=600,ServiceMinutes=10,PalletDemand=-10,WeightDemand=-700,RefrigeratedRequirement=1},
                new Job{Id=3,Name="Pickup B",Type=JobType.Pickup,ReadyTime=60,DueTime=540,ServiceMinutes=15,PalletDemand=+8,WeightDemand=+900},
                new Job{Id=4,Name="Delivery B",Type=JobType.Delivery,ReadyTime=150,DueTime=660,ServiceMinutes=15,PalletDemand=-8,WeightDemand=-900}
            },
            Vehicles = new List<Vehicle>
            {
                new Vehicle{Id=0,Name="Van A",SpeedFactor=1.0,ShiftLimitMinutes=360,DriverRatePerHour=80,MaintenanceRatePerHour=20,FuelRatePerKm=0.5,BaseFee=120,MaxPallets=12,MaxWeight=1200,RefrigeratedCapacity=0},
                new Vehicle{Id=1,Name="Truck B",SpeedFactor=1.3,ShiftLimitMinutes=480,DriverRatePerHour=100,MaintenanceRatePerHour=30,FuelRatePerKm=0.7,BaseFee=150,MaxPallets=20,MaxWeight=2000,RefrigeratedCapacity=9999},
                new Vehicle{Id=2,Name="Refrig C",SpeedFactor=1.1,ShiftLimitMinutes=420,DriverRatePerHour=90,MaintenanceRatePerHour=25,FuelRatePerKm=0.6,BaseFee=130,MaxPallets=15,MaxWeight=1500,RefrigeratedCapacity=9999}
            }
        };
    }

    // ------------------- Solve VRP -------------------
    static VrpResult SolveVrp(VrpInput data) {
        const double overtimeMult = 2.0;
        const long slackMax = 60;
        int depot = data.Jobs.Find(j => j.Type == JobType.Depot).Id;
        int vehicleCount = data.Vehicles.Count;

        var mgr = new RoutingIndexManager(data.Jobs.Count, vehicleCount, depot);
        var rt = new RoutingModel(mgr);
        var slv = rt.solver();

        // ---- Time callbacks per vehicle ----
        int[] timeCb = new int[vehicleCount];
        for (int v = 0; v < vehicleCount; v++) {
            var veh = data.Vehicles[v];
            timeCb[v] = rt.RegisterTransitCallback((long from, long to) => {
                int i = mgr.IndexToNode(from);
                int j = mgr.IndexToNode(to);
                return (long)Math.Round((data.TravelMinutes[i][j] + data.Jobs[i].ServiceMinutes) * veh.SpeedFactor);
            });
        }

        // ---- Cost callbacks per vehicle ----
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

        // ---- Capacity dimensions ----
        int palletCb = rt.RegisterUnaryTransitCallback(idx => data.Jobs[mgr.IndexToNode(idx)].PalletDemand);
        rt.AddDimensionWithVehicleCapacity(palletCb, 0, data.Vehicles.ConvertAll(v => v.MaxPallets).ToArray(), true, "Pallets");

        int weightCb = rt.RegisterUnaryTransitCallback(idx => data.Jobs[mgr.IndexToNode(idx)].WeightDemand);
        rt.AddDimensionWithVehicleCapacity(weightCb, 0, data.Vehicles.ConvertAll(v => v.MaxWeight).ToArray(), true, "Weight");

        int refrigCb = rt.RegisterUnaryTransitCallback(idx => data.Jobs[mgr.IndexToNode(idx)].RefrigeratedRequirement);
        rt.AddDimensionWithVehicleCapacity(refrigCb, 0, data.Vehicles.ConvertAll(v => v.RefrigeratedCapacity).ToArray(), true, "Refrig");

        // ---- Time dimension ----
        rt.AddDimensionWithVehicleTransits(timeCb, slackMax, 720, true, "Time");
        var dimTime = rt.GetMutableDimension("Time");

        foreach (var job in data.Jobs) {
            long idx = mgr.NodeToIndex(job.Id);
            dimTime.CumulVar(idx).SetRange(job.ReadyTime, job.DueTime);
        }

        // Overtime & base fee
        for (int v = 0; v < vehicleCount; v++) {
            var veh = data.Vehicles[v];
            double perMin = (veh.DriverRatePerHour + veh.MaintenanceRatePerHour) / 60.0;
            dimTime.SetCumulVarSoftUpperBound(rt.End(v), veh.ShiftLimitMinutes,
                (long)Math.Round(perMin * (overtimeMult - 1) * 1000));
            rt.SetFixedCostOfVehicle((long)Math.Round(veh.BaseFee * 1000), v);
        }

        // ---- Pickup-Delivery pairs (inferred from job order for demo) ----
        int[,] pairs = { { 1, 2 }, { 3, 4 } };
        for (int i = 0; i < pairs.GetLength(0); i++) {
            long pu = mgr.NodeToIndex(pairs[i, 0]);
            long dl = mgr.NodeToIndex(pairs[i, 1]);
            rt.AddPickupAndDelivery(pu, dl);
            slv.Add(rt.VehicleVar(pu) == rt.VehicleVar(dl));
            slv.Add(dimTime.CumulVar(pu) <= dimTime.CumulVar(dl));
        }

        // ---- Search parameters ----
        var search = operations_research_constraint_solver.DefaultRoutingSearchParameters();
        search.FirstSolutionStrategy = FirstSolutionStrategy.Types.Value.ParallelCheapestInsertion;
        search.LocalSearchMetaheuristic = LocalSearchMetaheuristic.Types.Value.GuidedLocalSearch;
        search.TimeLimit = new Duration { Seconds = 30 };
        search.UseDepthFirstSearch = true;
        search.LogSearch = false;

        var sol = rt.SolveWithParameters(search);
        if (sol == null) return new VrpResult();

        // ---- Build result ----
        var res = new VrpResult();
        var dimPal = rt.GetMutableDimension("Pallets");
        var dimWgt = rt.GetMutableDimension("Weight");
        var dimRef = rt.GetMutableDimension("Refrig");

        for (int v = 0; v < vehicleCount; v++) {
            var veh = data.Vehicles[v];
            var route = new VehicleRoute { VehicleId = v, VehicleName = veh.Name, Used = rt.IsVehicleUsed(sol, v) };
            if (!route.Used) { route.Stops.Add("(no jobs)"); res.Routes.Add(route); continue; }

            double perMin = (veh.DriverRatePerHour + veh.MaintenanceRatePerHour) / 60.0;
            double perKm = veh.FuelRatePerKm;
            double baseFee = veh.BaseFee;
            double time = 0, dist = 0;

            long idx = rt.Start(v);
            while (!rt.IsEnd(idx)) {
                int from = mgr.IndexToNode(idx);
                long next = sol.Value(rt.NextVar(idx));
                int to = mgr.IndexToNode(next);
                long arr = sol.Value(dimTime.CumulVar(idx));
                long pal = sol.Value(dimPal.CumulVar(idx));
                long wgt = sol.Value(dimWgt.CumulVar(idx));
                long refg = sol.Value(dimRef.CumulVar(idx));

                route.Stops.Add($"{from}:{data.Jobs[from].Name}(T={arr},P={pal},W={wgt},R={refg})");
                time += data.TravelMinutes[from][to] + data.Jobs[from].ServiceMinutes;
                dist += data.DistanceKm[from][to];
                idx = next;
            }
            route.Stops.Add("End");
            double cost = baseFee + time * perMin + dist * perKm;
            route.TotalMinutes = time;
            route.DistanceKm = dist;
            route.Cost = cost;
            res.Routes.Add(route);
            res.TotalCost += cost;
        }
        return res;
    }

    // ------------------- Print -------------------
    static void PrintResult(VrpResult res) {
        Console.WriteLine("\n========== VRP RESULT ==========");
        foreach (var r in res.Routes) {
            Console.WriteLine($"\n🚚 {r.VehicleName} (V{r.VehicleId}): {(r.Used ? "used" : "unused")}");
            Console.WriteLine("  " + string.Join(" → ", r.Stops));
            Console.WriteLine($"  Time: {r.TotalMinutes:F0} min  Dist: {r.DistanceKm:F1} km  Cost: {r.Cost:F2} rate units");
        }
        Console.WriteLine($"\n💰 Total Fleet Cost: {res.TotalCost:F2} rate units");
    }
}
