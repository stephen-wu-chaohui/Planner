using Planner.Contracts.Optimization.Inputs;

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

public static class VehicleFormMapper {
    public static VehicleInput ToInput(this VehicleFormModel v)
        => new(
            VehicleId: v.VehicleId,
            Name: v.Name,
            ShiftLimitMinutes: v.ShiftLimitMinutes,
            DepotStartId: v.DepotStartId,
            DepotEndId: v.DepotEndId,
            SpeedFactor: v.SpeedFactor,
            CostPerMinute: v.CostPerMinute,
            CostPerKm: v.CostPerKm,
            BaseFee: v.BaseFee,
            MaxPallets: v.MaxPallets,
            MaxWeight: v.MaxWeight,
            RefrigeratedCapacity: v.RefrigeratedCapacity
        );
}

