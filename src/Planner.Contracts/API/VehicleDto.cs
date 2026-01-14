using System;

namespace Planner.Contracts.API;

/// <summary>
/// Vehicle representation shared between API and clients.
/// </summary>
public sealed record VehicleDto(
    long Id,
    string Name,
    double SpeedFactor,
    long ShiftLimitMinutes,
    long DepotStartId,
    long DepotEndId,
    double DriverRatePerHour,
    double MaintenanceRatePerHour,
    double FuelRatePerKm,
    double BaseFee,
    long MaxPallets,
    long MaxWeight,
    long RefrigeratedCapacity);
