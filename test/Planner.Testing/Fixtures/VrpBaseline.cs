namespace Planner.Testing.Fixtures;

using Planner.Domain;
using Planner.Messaging.Optimization;
using Planner.Testing.Builders;

public static class VrpBaseline {
    public static OptimizeRouteRequest CreateSmallDeterministic() {
        return OptimizeRouteRequestBuilder.Create()
            .WithTenant(TestIds.TenantId)
            .WithRunId(TestIds.RunId)
            .WithJobs([ new Job { Id = TestIds.Job1, Name = "Job-1", Location = new Location { Id = TestIds.Job1Loc } },
                new Job { Id = TestIds.Job2, Name = "Job-2", Location = new Location { Id = TestIds.Job2Loc } } ])
            .WithVehicles([ new Vehicle { Id = TestIds.Vehicle1, Name = "Van-1", 
                StartDepot = new Depot { Location = new Location { Id = TestIds.Depot1Loc } }, 
                EndDepot = new Depot { Location = new Location { Id = TestIds.Depot2Loc } } } ])
            .WithOvertimeMultiplier(2.0)
            .Build();
    }
}
