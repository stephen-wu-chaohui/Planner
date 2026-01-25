using Planner.Messaging.Optimization.Inputs;

namespace Planner.Testing.Builders;

public sealed class JobInputBuilder {
    private int _jobId = TestIds.Job1;
    private int _locationType = 1; // match contract expectations (your controller maps domain enum -> int)
    private string _name = "Test Job";
    private long _location = LocationInputBuilder.Create().WithId(TestIds.Job1Loc).Build();

    private long _serviceTimeMinutes = 10;
    private long _readyTime = 0;
    private long _dueTime = 720;

    private long _palletDemand = 1;
    private long _weightDemand = 10;
    private bool _requiresRefrigeration = false;

    public static JobInputBuilder Create() => new();

    public JobInputBuilder WithJobId(int id) { _jobId = id; return this; }
    public JobInputBuilder WithJobType(int jobType) { _locationType = jobType; return this; }
    public JobInputBuilder WithName(string name) { _name = name; return this; }
    public JobInputBuilder WithLocation(long location) { _location = location; return this; }

    public JobInputBuilder WithTimeWindow(long ready, long due) { _readyTime = ready; _dueTime = due; return this; }
    public JobInputBuilder WithService(long minutes) { _serviceTimeMinutes = minutes; return this; }

    public JobInputBuilder WithDemand(long pallets, long weight) { _palletDemand = pallets; _weightDemand = weight; return this; }
    public JobInputBuilder RequiresRefrigeration(bool value = true) { _requiresRefrigeration = value; return this; }

    public StopInput Build() => new(
        LocationId: _location,
        LocationType: _locationType,
        ServiceTimeMinutes: _serviceTimeMinutes,
        ReadyTime: _readyTime,
        DueTime: _dueTime,
        PalletDemand: _palletDemand,
        WeightDemand: _weightDemand,
        RequiresRefrigeration: _requiresRefrigeration,
        ExtraIdForJob: _jobId
    );
}
