using System;

namespace Planner.Contracts.API;

public enum JobTypeDto { Depot = 0, Pickup = 1, Delivery = 2 }

/// <summary>
/// Job representation used by API endpoints.
/// </summary>
public sealed record JobDto(
    long Id,
    string Name,
    long OrderId,
    long CustomerId,
    JobTypeDto JobType,
    string Reference,
    LocationDto Location,
    long ServiceTimeMinutes,
    long PalletDemand,
    long WeightDemand,
    long ReadyTime,
    long DueTime,
    bool RequiresRefrigeration);
