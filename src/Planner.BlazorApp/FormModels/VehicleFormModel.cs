using Planner.Contracts.API;

namespace Planner.BlazorApp.FormModels;

public sealed class VehicleFormModel : EditableFlags {
    public long VehicleId { get; set; } = 0;
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

    public VehicleFormModel() { }

    public VehicleFormModel(VehicleFormModel other) : base(other) {
        VehicleId = other.VehicleId;
        Name = other.Name;
        ShiftLimitMinutes = other.ShiftLimitMinutes;
        DepotStartId = other.DepotStartId;
        DepotEndId = other.DepotEndId;
        SpeedFactor = other.SpeedFactor;
        CostPerMinute = other.CostPerMinute;
        CostPerKm = other.CostPerKm;
        BaseFee = other.BaseFee;
        MaxPallets = other.MaxPallets;
        MaxWeight = other.MaxWeight;
        RefrigeratedCapacity = other.RefrigeratedCapacity;
    }
}

/// <summary>
/// Maps UI Vehicle form models to contracts.
/// Maps contracts to UI Vehicle form models.
/// </summary>

public static class VehicleFormMapper {
    public static VehicleDto ToDto(this VehicleFormModel v) {
        return new VehicleDto(
            Id: Math.Max(v.VehicleId, 0),
            Name: v.Name,
            SpeedFactor: v.SpeedFactor,
            ShiftLimitMinutes: v.ShiftLimitMinutes,
            DepotStartId: v.DepotStartId,
            DepotEndId: v.DepotEndId,
            DriverRatePerHour: v.CostPerMinute * 60.0,
            MaintenanceRatePerHour: 0.0,
            FuelRatePerKm: v.CostPerKm,
            BaseFee: v.BaseFee,
            MaxPallets: v.MaxPallets,
            MaxWeight: v.MaxWeight,
            RefrigeratedCapacity: v.RefrigeratedCapacity
        );
    }

    public static VehicleFormModel ToFormModel(this VehicleDto dto) {
        return new VehicleFormModel {
            VehicleId = dto.Id,
            Name = dto.Name,
            ShiftLimitMinutes = dto.ShiftLimitMinutes,
            DepotStartId = dto.DepotStartId,
            DepotEndId = dto.DepotEndId,
            SpeedFactor = dto.SpeedFactor,
            CostPerMinute = dto.DriverRatePerHour / 60.0,
            CostPerKm = dto.FuelRatePerKm,
            BaseFee = dto.BaseFee,
            MaxPallets = dto.MaxPallets,
            MaxWeight = dto.MaxWeight,
            RefrigeratedCapacity = dto.RefrigeratedCapacity
        };
    }

}

