using Planner.Messaging.Optimization.Inputs;

namespace Planner.Testing.Builders;

public sealed class LocationInputBuilder {
    private long _locationId = 1;
    private string _address = "Test Address";
    private double _lat = -31.9505;  // Perth-ish
    private double _lng = 115.8605;

    public static LocationInputBuilder Create() => new();

    public LocationInputBuilder WithId(long id) { _locationId = id; return this; }
    public LocationInputBuilder WithAddress(string address) { _address = address; return this; }
    public LocationInputBuilder WithLatLng(double lat, double lng) { _lat = lat; _lng = lng; return this; }

    public long Build() => _locationId;
}
