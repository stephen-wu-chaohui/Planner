namespace Planner.Domain;

public sealed class Location {
    public long Id { get; set; }
    public string Address { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }

    public Location(long id, string address, double latitude, double longitude) {
        if (latitude is < -90 or > 90)
            throw new ArgumentOutOfRangeException(nameof(latitude));

        if (longitude is < -180 or > 180)
            throw new ArgumentOutOfRangeException(nameof(longitude));

        Id = id;
        Address = address;
        Latitude = latitude;
        Longitude = longitude;
    }

    // Parameterless ctor for EF
    public Location() { }
}