using FluentAssertions;
using Planner.Contracts.Messages.LinearSolver;
using Planner.Optimization.LinearSolver;

namespace Planner.Optimization.Tests.LinearSolver;


public record LinearSolverTestCase {
    public required string Description { get; init; }
    public required LinearSolverRequest Request { get; init; }

    /// <summary>
    /// The expected solver results (variables, constraints, objective, status).
    /// </summary>
    public required LinearSolverResponse ExpectedResults { get; init; }
}

public class LinearSolverTests {
    [Fact]
    public void Should_Solve_Simple_Maximization() {
        // Arrange
        var request = new LinearSolverRequest {
            Algorithm = "CBC_MIXED_INTEGER_PROGRAMMING",
            Variables =
            [
                new LinearVariable { Name = "A", LowerBound = 0, UpperBound = 10, IsInteger = false },
                new LinearVariable { Name = "B", LowerBound = 0, UpperBound = 10, IsInteger = false }
            ],
            Objectives =
            [
                new LinearExpression
                {
                    Name = "Profit",
                    Coefficients = new[] { 3.0, 4.0 },
                    Direction = LinearSolverDirection.Maximize
                }
            ],
            Constraints =
            [
                new LinearExpression
                {
                    Name = "Material",
                    Coefficients = new[] { 1.0, 2.0 },
                    UpperBound = 8.0
                },
                new LinearExpression
                {
                    Name = "Time",
                    Coefficients = new[] { 3.0, 1.0 },
                    UpperBound = 6.0
                }
            ]
        };

        // Act
        var result = LinearSolverBuilder.Solve(request);

        // Assert
        result.Status.Should().Be("OPTIMAL");
        result.ObjectiveValue.Should().BeApproximately(16.8, 1e-6);
        result.Variables.Should().ContainSingle(v => v.Name == "A" && v.Value == 0.8);
        result.Variables.Should().ContainSingle(v => v.Name == "B" && v.Value == 3.6);
    }

    [Fact]
    public void Should_Ignore_Inactive_Constraints() {
        // Arrange
        var request = new LinearSolverRequest {
            Algorithm = "GLOP_LINEAR_PROGRAMMING",
            Variables =
            [
                new LinearVariable { Name = "X", LowerBound = 0, UpperBound = 10 }
            ],
            Objectives =
            [
                new LinearExpression
                {
                    Coefficients = new[] { 1.0 },
                    Direction = LinearSolverDirection.Maximize
                }
            ],
            Constraints =
            [
                new LinearExpression
                {
                    Name = "Active",
                    Coefficients = new[] { 1.0 },
                    UpperBound = 5.0,
                    IsActive = true
                },
                new LinearExpression
                {
                    Name = "Inactive",
                    Coefficients = new[] { 1.0 },
                    UpperBound = 3.0,
                    IsActive = false
                }
            ]
        };

        // Act
        var result = LinearSolverBuilder.Solve(request);

        // Assert
        result.Variables.First().Value.Should().BeApproximately(5.0, 1e-6, "Inactive constraint should not limit the variable");
    }

    [Fact]
    public void Should_Combine_Multi_Objectives_By_Weight() {
        // Arrange
        var request = new LinearSolverRequest {
            Algorithm = "GLOP_LINEAR_PROGRAMMING",
            Variables =
            [
                new LinearVariable { Name = "A", LowerBound = 0, UpperBound = 10 },
                new LinearVariable { Name = "B", LowerBound = 0, UpperBound = 10 }
            ],
            Objectives =
            [
                new LinearExpression
                {
                    Name = "Profit",
                    Coefficients = new[] { 3.0, 4.0 },
                    Direction = LinearSolverDirection.Maximize,
                    Weight = 1.0
                },
                new LinearExpression
                {
                    Name = "Cost",
                    Coefficients = new[] { -1.0, -2.0 },
                    Direction = LinearSolverDirection.Maximize,
                    Weight = 0.5
                }
            ],
            Constraints =
            [
                new LinearExpression
                {
                    Name = "Limit",
                    Coefficients = new[] { 1.0, 1.0 },
                    UpperBound = 8.0
                }
            ]
        };

        // Act
        var result = LinearSolverBuilder.Solve(request);

        // Assert
        result.Status.Should().Be("OPTIMAL");
        result.ObjectiveValue.Should().BeGreaterThan(0);
        result.Variables.Should().HaveCount(2);
    }

    [Fact]
    public void Should_Return_Slack_And_DualValues_For_Constraints() {
        // Arrange
        var request = new LinearSolverRequest {
            Algorithm = "GLOP_LINEAR_PROGRAMMING",
            Variables =
            [
                new LinearVariable { Name = "X", LowerBound = 0, UpperBound = 10 },
                new LinearVariable { Name = "Y", LowerBound = 0, UpperBound = 10 }
            ],
            Objectives =
            [
                new LinearExpression
                {
                    Coefficients = new[] { 3.0, 4.0 },
                    Direction = LinearSolverDirection.Maximize
                }
            ],
            Constraints =
            [
                new LinearExpression
                {
                    Name = "Bound",
                    Coefficients = new[] { 1.0, 1.0 },
                    UpperBound = 8.0
                }
            ]
        };

        // Act
        var result = LinearSolverBuilder.Solve(request);

        // Assert
        result.Constraints.Should().ContainSingle();
        var constraint = result.Constraints.First();

        constraint.Slack.Should().BeGreaterThanOrEqualTo(0);
        constraint.DualValue.Should().BeGreaterThanOrEqualTo(0);
    }

    [Theory]
    [ClassData(typeof(LinearSolverRequestDataFromJson))]
    public void Should_Match_Expected_SolverResponses(LinearSolverTestCase test) {
        // Act
        var actual = LinearSolverBuilder.Solve(test.Request);
        var expected = test.ExpectedResults;

        // Assert
        actual.Status.Should().Be(expected.Status, because: test.Description);
        actual.ObjectiveValue.Should().BeApproximately(expected.ObjectiveValue, 0.1, $"Objective in {test.Description}");

        // Variables
        foreach (var expVar in expected.Variables) {
            var actVar = actual.Variables.FirstOrDefault(v => v.Name == expVar.Name)
                         ?? throw new Exception($"Variable {expVar.Name} missing in {test.Description}");
            actVar.Value.Should().BeApproximately(expVar.Value, 0.05, $"Var {expVar.Name} in {test.Description}");
        }

        // Constraints (if provided)
        foreach (var expCon in expected.Constraints) {
            var actCon = actual.Constraints.FirstOrDefault(c => c.Name == expCon.Name)
                         ?? throw new Exception($"Constraint {expCon.Name} missing in {test.Description}");
            actCon.LhsValue.Should().BeApproximately(expCon.LhsValue, 0.1, $"LHS {expCon.Name}");
            actCon.Slack.Should().BeApproximately(expCon.Slack, 0.1, $"Slack {expCon.Name}");
        }
    }

}
