using Planner.Messaging.ContractSnapshots.Snapshot;
using Planner.Testing.Fixtures;
using System.Threading.Tasks;
using VerifyXunit;
using Xunit;

namespace Planner.Messaging.ContractSnapshots.Tests;

public sealed class OptimizeRouteRequestContractSnapshotTests
{
    [Fact]
    public async Task OptimizeRouteRequest_contract_is_stable() {
        var request = VrpBaseline.CreateSmallDeterministic();

        var snapshot = OptimizeRouteRequestSnapshot.Create(request);

        await Verifier.Verify(snapshot);

    }
}
