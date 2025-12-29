namespace Planner.Testing.Fixtures;

using Planner.Testing.Builders;

public static class VrpBaseline {
    public static OptimizeRouteRequest CreateSmallDeterministic() {
        var depot1 = DepotInputBuilder.Create()
            .WithLocation(LocationInputBuilder.Create()
                .WithId(TestIds.Depot1Loc)
                .WithAddress("Depot-1")
                .WithLatLng(-31.9505, 115.8605)
                .Build())
            .Build();

        var depot2 = DepotInputBuilder.Create()
            .WithLocation(LocationInputBuilder.Create()
                .WithId(TestIds.Depot2Loc)
                .WithAddress("Depot-2")
                .WithLatLng(-31.9520, 115.8610)
                .Build())
            .Build();

        var job1 = JobInputBuilder.Create()
            .WithJobId(TestIds.Job1)
            .WithName("Job-1")
            .WithLocation(LocationInputBuilder.Create()
                .WithId(TestIds.Job1Loc)
                .WithAddress("Job-1 Addr")
                .WithLatLng(-31.9490, 115.8590)
                .Build())
            .WithTimeWindow(0, 720)
            .WithService(10)
            .WithDemand(1, 10)
            .Build();

        var job2 = JobInputBuilder.Create()
            .WithJobId(TestIds.Job2)
            .WithName("Job-2")
            .WithLocation(LocationInputBuilder.Create()
                .WithId(TestIds.Job2Loc)
                .WithAddress("Job-2 Addr")
                .WithLatLng(-31.9480, 115.8625)
                .Build())
            .WithTimeWindow(0, 720)
            .WithService(10)
            .WithDemand(1, 10)
            .Build();

        var vehicle1 = VehicleInputBuilder.Create()
            .WithVehicleId(TestIds.Vehicle1)
            .WithName("Van-1")
            .WithDepot(TestIds.Depot1Loc, TestIds.Depot1Loc)
            .WithCapacity(pallets: 10, weight: 1000)
            .WithCosts(perMin: 1.0, perKm: 1.0, baseFee: 0)
            .Build();

        return OptimizeRouteRequestBuilder.Create()
            .WithTenant(TestIds.TenantId)
            .WithRunId(TestIds.RunId)
            .WithDepots(new[] { depot1, depot2 })
            .WithJobs(new[] { job1, job2 })
            .WithVehicles(new[] { vehicle1 })
            .WithOvertimeMultiplier(2.0)
            .Build();
    }
}
