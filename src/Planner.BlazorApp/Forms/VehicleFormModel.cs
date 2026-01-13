using Planner.Domain;
namespace Planner.BlazorApp.Forms;

public sealed class VehicleFormModel {
    public int VehicleId { get; set; }
    public string Name { get; set; } = string.Empty;

    public long ShiftLimitMinutes { get; set; }
    public long DepotStartId { get; set; }
    public long DepotEndId { get; set; }

    public double SpeedFactor { get; set; }

    public double CostPerMinute { get; set; }
    public double CostPerKm { get; set; }
    public double BaseFee { get; set; }

    public long MaxPallets { get; set; }
    public long MaxWeight { get; set; }
    public long RefrigeratedCapacity { get; set; }
}

/// <summary>
/// Maps UI Vehicle form models to contracts.  Use Domain.Vehicle as the contract here.
/// Maps contracts to UI Vehicle form models.  Use Domain.Vehicle as the contract here.
/// </summary>

public static class VehicleFormMapper {
    public static Vehicle ToContract(this VehicleFormModel v) {
        return new Vehicle {
            Id = v.VehicleId,
            Name = v.Name,
            ShiftLimitMinutes = v.ShiftLimitMinutes,
            DepotStartId = v.DepotStartId,
            DepotEndId = v.DepotEndId,
            SpeedFactor = v.SpeedFactor,
            DriverRatePerHour = v.CostPerMinute * 60.0,
            MaintenanceRatePerHour = 0.0,
            FuelRatePerKm = v.CostPerKm,
            BaseFee = v.BaseFee,
            MaxPallets = v.MaxPallets,
            MaxWeight = v.MaxWeight,
            RefrigeratedCapacity = v.RefrigeratedCapacity
        };
    }

    public static IReadOnlyList<Vehicle> ToContracts(IEnumerable<VehicleFormModel> forms) {
        ArgumentNullException.ThrowIfNull(forms);
        return [.. forms.Select(ToContract)];
    }

    public static VehicleFormModel ToFormModel(this Vehicle v) {
        return new VehicleFormModel {
            VehicleId = (int)v.Id,
            Name = v.Name,
            ShiftLimitMinutes = v.ShiftLimitMinutes,
            DepotStartId = v.DepotStartId,
            DepotEndId = v.DepotEndId,
            SpeedFactor = v.SpeedFactor,
            CostPerMinute = v.DriverRatePerHour / 60.0,
            CostPerKm = v.FuelRatePerKm,
            BaseFee = v.BaseFee,
            MaxPallets = v.MaxPallets,
            MaxWeight = v.MaxWeight,
            RefrigeratedCapacity = v.RefrigeratedCapacity
        };
    }
}

