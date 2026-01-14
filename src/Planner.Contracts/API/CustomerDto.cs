using System;

namespace Planner.Contracts.API;

/// <summary>
/// Customer representation shared between API and clients.
/// </summary>
/// <param name="CustomerId">Tenant-scoped customer identifier.</param>
/// <param name="Name">Customer display name.</param>
/// <param name="Location">Customer address and coordinates.</param>
/// <param name="DefaultServiceMinutes">Default service duration in minutes.</param>
/// <param name="RequiresRefrigeration">Indicates if the customer needs refrigeration by default.</param>
public sealed record CustomerDto(
    long CustomerId,
    string Name,
    LocationDto Location,
    long DefaultServiceMinutes,
    bool RequiresRefrigeration);
