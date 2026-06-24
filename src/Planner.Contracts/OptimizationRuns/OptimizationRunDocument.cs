using System.Text.Json.Serialization;
using Newtonsoft.Json;
using Planner.Messaging.Optimization.Inputs;
using Planner.Messaging.Optimization.Outputs;

namespace Planner.Contracts.OptimizationRuns;

public sealed record OptimizationRunDocument(
    [property: JsonPropertyName("id")]
    [property: JsonProperty("id")]
    string Id,

    [property: JsonPropertyName("tenantId")]
    [property: JsonProperty("tenantId")]
    Guid TenantId,

    [property: JsonPropertyName("optimizationRunId")]
    [property: JsonProperty("optimizationRunId")]
    Guid OptimizationRunId,

    [property: JsonPropertyName("schemaVersion")]
    [property: JsonProperty("schemaVersion")]
    int SchemaVersion,

    [property: JsonPropertyName("version")]
    [property: JsonProperty("version")]
    long Version,

    [property: JsonPropertyName("status")]
    [property: JsonProperty("status")]
    OptimizationRunStatus Status,

    [property: JsonPropertyName("requestedAtUtc")]
    [property: JsonProperty("requestedAtUtc")]
    DateTime RequestedAtUtc,

    [property: JsonPropertyName("updatedAtUtc")]
    [property: JsonProperty("updatedAtUtc")]
    DateTime UpdatedAtUtc,

    [property: JsonPropertyName("requestSnapshot")]
    [property: JsonProperty("requestSnapshot")]
    OptimizeRouteRequest RequestSnapshot,

    [property: JsonPropertyName("summary")]
    [property: JsonProperty("summary")]
    OptimizationRunSummaryDto Summary,

    [property: JsonPropertyName("solverResult")]
    [property: JsonProperty("solverResult")]
    OptimizeRouteResponse? SolverResult,

    [property: JsonPropertyName("aiInsight")]
    [property: JsonProperty("aiInsight")]
    OptimizationAiInsightDto? AiInsight,

    [property: JsonPropertyName("timeline")]
    [property: JsonProperty("timeline")]
    IReadOnlyList<OptimizationRunTimelineEventDto> Timeline,

    [property: JsonPropertyName("attempts")]
    [property: JsonProperty("attempts")]
    IReadOnlyList<OptimizationRunAttemptDto> Attempts,

    [property: JsonPropertyName("errorMessage")]
    [property: JsonProperty("errorMessage")]
    string? ErrorMessage);
