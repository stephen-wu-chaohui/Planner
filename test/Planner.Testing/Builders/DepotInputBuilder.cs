using Planner.Messaging.Optimization.Inputs;

namespace Planner.Testing.Builders;

// Depots are derived from vehicle StartLocation/EndLocation in the solver.
// This builder remains for backwards compatibility in tests.
public sealed class DepotInputBuilder {
    private long _location = LocationInputBuilder.Create().WithId(TestIds.Depot1Loc).Build();

    public static DepotInputBuilder Create() => new();

    public DepotInputBuilder WithLocation(long location) { _location = location; return this; }
    public long Build() => _location;
}
