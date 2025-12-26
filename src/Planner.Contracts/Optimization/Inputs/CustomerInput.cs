namespace Planner.Contracts.Optimization.Inputs;

/// <summary>
/// Customer/location input used to construct optimization jobs.
/// Immutable and safe for transport across UI, API, and services.
/// </summary>
public sealed record CustomerInput(
    long CustomerId,
    string Name,
    LocationInput Location,
    long DefaultServiceMinutes,
    bool RequiresRefrigeration,
    int DefaultJobType
);
