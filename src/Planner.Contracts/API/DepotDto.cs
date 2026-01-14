using System;

namespace Planner.Contracts.API;

/// <summary>
/// Depot representation for API contracts.
/// </summary>
/// <param name="Id">Depot identifier.</param>
/// <param name="Name">Depot display name.</param>
/// <param name="Location">Physical location of the depot.</param>
public sealed record DepotDto(
    long Id,
    string Name,
    LocationDto Location);
