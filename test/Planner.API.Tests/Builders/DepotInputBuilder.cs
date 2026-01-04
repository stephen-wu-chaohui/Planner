namespace Planner.Testing.Builders;

// Depots are derived from vehicle StartLocation/EndLocation in the solver.
// This builder is kept to avoid breaking references if any remain, but is no longer used.
public sealed class DepotInputBuilder {
    private LocationInput _location = LocationInputBuilder.Create().WithId(TestIds.Depot1Loc).Build();

    public static DepotInputBuilder Create() => new();

    public DepotInputBuilder WithLocation(LocationInput location) { _location = location; return this; }

    public DepotInput Build() => new(_location);
}
