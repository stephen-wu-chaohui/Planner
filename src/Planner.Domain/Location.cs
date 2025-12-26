namespace Planner.Domain;

public sealed class Location {
    public long Id { get; private set; }
    public double Latitude { get; private set; }
    public double Longitude { get; private set; }

    public Location(long id, double latitude, double longitude) {
        if (latitude is < -90 or > 90)
            throw new ArgumentOutOfRangeException(nameof(latitude));

        if (longitude is < -180 or > 180)
            throw new ArgumentOutOfRangeException(nameof(longitude));

        Id = id;
        Latitude = latitude;
        Longitude = longitude;
    }

    // Parameterless ctor for EF
    private Location() { }
}