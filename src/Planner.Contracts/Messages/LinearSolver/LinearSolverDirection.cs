using System.Text.Json.Serialization;

namespace Planner.Contracts.Messages.LinearSolver;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum LinearSolverDirection {
    Maximize,
    Minimize
}
