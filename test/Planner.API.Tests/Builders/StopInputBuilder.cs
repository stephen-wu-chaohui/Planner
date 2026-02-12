namespace Planner.Testing.Builders;

public sealed class StopInputBuilder
{
    private int _stopId = 1;
    private string _name = "Stop 1";
    private LocationInput _location = LocationInputBuilder.Create().Build();
    private long _serviceTimeMinutes = 10;
    private long _readyTime = 0;
    private long _dueTime = 720;
    private int _stopType = 1;

    public static StopInputBuilder Create() => new();

    public StopInputBuilder WithStopId(int id) { _stopId = id; return this; }
    public StopInputBuilder WithName(string name) { _name = name; return this; }
    public StopInputBuilder WithLocation(LocationInput location) { _location = location; return this; }
    public StopInputBuilder WithServiceTime(long minutes) { _serviceTimeMinutes = minutes; return this; }
    public StopInputBuilder WithTimeWindow(long ready, long due) { _readyTime = ready; _dueTime = due; return this; }
    public StopInputBuilder WithStopType(int stopType) { _stopType = stopType; return this; }

    public StopInput Build() => new(
        StopId: _stopId,
        Name: _name,
        Location: _location,
        ServiceTimeMinutes: _serviceTimeMinutes,
        ReadyTime: _readyTime,
        DueTime: _dueTime,
        StopType: _stopType
    );
}
