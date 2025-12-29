using Planner.Testing.Fixtures;
using System.Threading.Tasks;
using VerifyXunit;
using Xunit;

namespace Planner.Optimization.SnapshotTests;

public sealed class VehicleRoutingProblemSnapshotTests
{
    public VehicleRoutingProblemSnapshotTests() : base() { }

    [Fact]
    public async Task Small_baseline_response_snapshot()
    {
        var solver = new VehicleRoutingProblem();
        var request = VrpBaseline.CreateSmallDeterministic();

        var response = solver.Optimize(request);
        var normalized = ResponseNormalizer.Normalize(response);

        await Verifier.Verify(normalized);
    }
}
