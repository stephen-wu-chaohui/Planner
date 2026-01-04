namespace Planner.Testing.Builders;

public sealed class VehicleInputBuilder {
    private int _vehicleId = TestIds.Vehicle1;
    private string _name = "Vehicle-1";

    private long _shiftLimitMinutes = 480;
    private long _depotStartId = TestIds.Depot1Loc;
    private long _depotEndId = TestIds.Depot1Loc;

    private double _speedFactor = 1.0;
    private double _costPerMinute = 1.0;
    private double _costPerKm = 1.0;
    private double _baseFee = 0;

    private long _maxPallets = 10;
    private long _maxWeight = 1000;
    private long _refrigeratedCapacity = 0;

    public static VehicleInputBuilder Create() => new();

    public VehicleInputBuilder WithVehicleId(int id) { _vehicleId = id; return this; }
    public VehicleInputBuilder WithName(string name) { _name = name; return this; }

    public VehicleInputBuilder WithDepot(long startId, long endId) { _depotStartId = startId; _depotEndId = endId; return this; }
    public VehicleInputBuilder WithShiftLimit(long minutes) { _shiftLimitMinutes = minutes; return this; }

    public VehicleInputBuilder WithCosts(double perMin, double perKm, double baseFee = 0) {
        _costPerMinute = perMin; _costPerKm = perKm; _baseFee = baseFee; return this;
    }

    public VehicleInputBuilder WithCapacity(long pallets, long weight, long refrig = 0) {
        _maxPallets = pallets; _maxWeight = weight; _refrigeratedCapacity = refrig; return this;
    }

    public VehicleInputBuilder WithSpeedFactor(double speedFactor) { _speedFactor = speedFactor; return this; }

    public VehicleInput Build() => new(
        VehicleId: _vehicleId,
        Name: _name,
        ShiftLimitMinutes: _shiftLimitMinutes,
        StartLocation: new LocationInput(
            LocationId: _depotStartId,
            Address: "DepotStart",
            Latitude: 0,
            Longitude: 0
        ),
        EndLocation: new LocationInput(
            LocationId: _depotEndId,
            Address: "DepotEnd",
            Latitude: 0,
            Longitude: 0
        ),
        SpeedFactor: _speedFactor,
        CostPerMinute: _costPerMinute,
        CostPerKm: _costPerKm,
        BaseFee: _baseFee,
        MaxPallets: _maxPallets,
        MaxWeight: _maxWeight,
        RefrigeratedCapacity: _refrigeratedCapacity
    );
}
