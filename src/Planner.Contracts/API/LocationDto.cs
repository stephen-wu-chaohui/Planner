using System;

namespace Planner.Contracts.API;

/// <summary>
/// Location value object used in API payloads.
/// </summary>
/// <param name="Id">Location identifier.</param>
/// <param name="Address">Street address or label.</param>
/// <param name="Latitude">Latitude in decimal degrees.</param>
/// <param name="Longitude">Longitude in decimal degrees.</param>
public sealed record LocationDto(
    long Id,
    string Address,
    double Latitude,
    double Longitude);
