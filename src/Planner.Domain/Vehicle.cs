namespace Planner.Domain;

public class Vehicle {
    public long Id { get; set; }
    public Guid TenantId { get; init; }

    public string Name { get; set; } = string.Empty;
    public double SpeedFactor { get; set; } = 1.0;
    public long ShiftLimitMinutes { get; set; } = 480;

    public long DepotStartId { get; set; }
    public long DepotEndId { get; set; }

    public Depot? StartDepot { get; set; }
    public Depot? EndDepot { get; set; }

    // cost parameters
    public double DriverRatePerHour { get; set; }
    public double MaintenanceRatePerHour { get; set; }
    public double FuelRatePerKm { get; set; }
    public double BaseFee { get; set; }

    // capacity constraints
    public long MaxPallets { get; set; }
    public long MaxWeight { get; set; }
    public long RefrigeratedCapacity { get; set; }
}
