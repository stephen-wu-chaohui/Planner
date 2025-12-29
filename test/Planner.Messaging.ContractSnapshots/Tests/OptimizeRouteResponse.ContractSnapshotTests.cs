using Planner.Messaging.ContractSnapshots.Snapshot;
using Planner.Optimization;
using Planner.Testing.Fixtures;
using System.Threading.Tasks;
using VerifyXunit;
using Xunit;

namespace Planner.Messaging.ContractSnapshots.Tests;

public sealed class OptimizeRouteResponseContractSnapshotTests
{
    [Fact]
    public async Task OptimizeRouteResponse_contract_is_stable() {
        var solver = new VehicleRoutingProblem();
        var request = VrpBaseline.CreateSmallDeterministic();

        var response = solver.Optimize(request);

        var snapshot = OptimizeRouteResponseSnapshot.Create(response);

        await Verifier.Verify(snapshot);
    }
}
