using System.Text.Json;
using FluentAssertions;
using Planner.Contracts.OptimizationRuns;
using Planner.Testing.Fixtures;
using Xunit;

namespace Planner.Orchestration.Tests;

public sealed class OptimizationRunContractTests {
    [Fact]
    public void OptimizationJobMessage_serializes_as_lightweight_body() {
        var message = new OptimizationJobMessage(
            Guid.Parse("00000000-0000-0000-0000-000000000001"),
            Guid.Parse("00000000-0000-0000-0000-000000000002"));

        var json = JsonSerializer.Serialize(message, new JsonSerializerOptions(JsonSerializerDefaults.Web));

        json.Should().Be("""{"tenantId":"00000000-0000-0000-0000-000000000001","optimizationRunId":"00000000-0000-0000-0000-000000000002"}""");
    }

    [Fact]
    public void SignalR_run_changed_dto_is_lightweight() {
        var run = CreateRun() with {
            SolverResult = new(
                Guid.Parse("00000000-0000-0000-0000-000000000001"),
                Guid.Parse("00000000-0000-0000-0000-000000000002"),
                DateTime.UtcNow,
                [],
                0)
        };

        var dto = run.ToRunChangedDto();
        var json = JsonSerializer.Serialize(dto, new JsonSerializerOptions(JsonSerializerDefaults.Web));

        dto.HasResult.Should().BeTrue();
        json.Should().Contain("optimizationRunId");
        json.Should().NotContain("routes");
        json.Should().NotContain("requestSnapshot");
        json.Should().NotContain("solverResult");
    }

    private static OptimizationRunDocument CreateRun() {
        var request = VrpBaseline.CreateSmallDeterministic();
        return new OptimizationRunDocument(
            Id: request.OptimizationRunId.ToString(),
            TenantId: request.TenantId,
            OptimizationRunId: request.OptimizationRunId,
            SchemaVersion: 1,
            Version: 1,
            Status: OptimizationRunStatus.Created,
            RequestedAtUtc: request.RequestedAt,
            UpdatedAtUtc: request.RequestedAt,
            RequestSnapshot: request,
            Summary: new OptimizationRunSummaryDto(
                request.Stops.Length,
                request.Vehicles.Length,
                request.RequestedAt,
                request.Settings?.SearchTimeLimitSeconds ?? 0,
                RequestedBy: "test@example.com"),
            SolverResult: null,
            AiInsight: null,
            Timeline: [],
            Attempts: [],
            ErrorMessage: null);
    }
}
