namespace Planner.Testing.Builders;

public sealed class DepotInputBuilder {
    private LocationInput _location = LocationInputBuilder.Create().WithId(TestIds.Depot1Loc).Build();

    public static DepotInputBuilder Create() => new();

    public DepotInputBuilder WithLocation(LocationInput location) { _location = location; return this; }

    public DepotInput Build() => new(_location);
}
