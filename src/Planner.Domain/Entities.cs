namespace Planner.Domain.Entities;

public class UserAccount {
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class SystemEvent {
    public int Id { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string Source { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public bool IsError { get; set; }
}

public class TaskItem {
    public int Id { get; set; }
    public string? Title { get; set; }
    public string? Description { get; set; }
    public DateTime DueDate { get; set; }
    public bool IsCompleted { get; set; } = false;
}

public class Customer {
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string DefaultJobType { get; set; } = "Delivery"; // optional
}

public enum JobType { Depot, Pickup, Delivery }

public class Job {
    public int Id { get; set; }           // matches Customer.Id
    public string Name { get; set; } = ""; // e.g. “Customer A — Morning Delivery”
    public int CustomerId { get; set; }
    public JobType Type { get; set; } = JobType.Delivery;
    public long ReadyTime { get; set; }
    public long DueTime { get; set; }
    public double ServiceMinutes { get; set; }
    public long PalletDemand { get; set; }
    public long WeightDemand { get; set; }
    public long RefrigeratedRequirement { get; set; }
    public int? VehiclePreference { get; set; }
}


public class Vehicle {
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public double SpeedFactor { get; set; } = 1.0;
    public long ShiftLimitMinutes { get; set; } = 480;
    public long DepotStartId { get; set; } = 0;
    public long DepotEndId { get; set; } = 0;

    // cost parameters (rates & fees)
    public double DriverRatePerHour { get; set; }
    public double MaintenanceRatePerHour { get; set; }
    public double FuelRatePerKm { get; set; }
    public double BaseFee { get; set; }

    // capacity constraints
    public long MaxPallets { get; set; }
    public long MaxWeight { get; set; }
    public long RefrigeratedCapacity { get; set; } = 0;
}


