using System.Text.Json;
using FluentAssertions;
using Planner.Contracts.OptimizationRuns;
using Planner.Testing.Fixtures;
using Xunit;

namespace Planner.Reactor.Tests;

public sealed class ReactorNotificationMappingTests {
    [Fact]
    public void Insight_changed_notification_does_not_include_markdown_analysis() {
        var request = VrpBaseline.CreateSmallDeterministic();
        var run = new OptimizationRunDocument(
            Id: request.OptimizationRunId.ToString(),
            TenantId: request.TenantId,
            OptimizationRunId: request.OptimizationRunId,
            SchemaVersion: 1,
            Version: 2,
            Status: OptimizationRunStatus.Succeeded,
            RequestedAtUtc: request.RequestedAt,
            UpdatedAtUtc: DateTime.UtcNow,
            RequestSnapshot: request,
            Summary: new OptimizationRunSummaryDto(request.Stops.Length, request.Vehicles.Length, request.RequestedAt, 5, null),
            SolverResult: null,
            AiInsight: new OptimizationAiInsightDto("completed", "## Long markdown", DateTime.UtcNow, null),
            Timeline: [],
            Attempts: [],
            ErrorMessage: null);

        var dto = run.ToInsightChangedDto();
        var json = JsonSerializer.Serialize(dto, new JsonSerializerOptions(JsonSerializerDefaults.Web));

        dto.HasAiInsight.Should().BeTrue();
        json.Should().Contain("insightStatus");
        json.Should().NotContain("markdown");
        json.Should().NotContain("Long markdown");
    }
}
