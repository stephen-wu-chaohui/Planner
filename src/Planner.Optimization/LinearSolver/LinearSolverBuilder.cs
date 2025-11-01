using Google.OrTools.LinearSolver;
using Planner.Contracts.Messages;
using Planner.Contracts.Messages.LinearSolver;

namespace Planner.Optimization.LinearSolver;

public static class LinearSolverBuilder
{
    public static Solver BuildSolver(LinearSolverRequest request, out Variable[] variables)
    {
        Solver solver = Solver.CreateSolver(request.Algorithm)
            ?? throw new InvalidOperationException($"Unsupported algorithm: {request.Algorithm}");

        // Apply solver parameters
        if (request.Parameters is { } p)
        {
            if (p.EnableOutput)
                solver.EnableOutput();

            if (p.TimeLimitMs.HasValue)
                solver.SetTimeLimit(p.TimeLimitMs.Value);

            if (p.Tolerance.HasValue)
                solver.SetSolverSpecificParametersAsString($"feasibility_tolerance={p.Tolerance.Value}");

            if (p.RandomSeed.HasValue)
                solver.SetSolverSpecificParametersAsString($"random_seed={p.RandomSeed.Value}");

            if (p.NumThreads.HasValue)
                solver.SetSolverSpecificParametersAsString($"threads={p.NumThreads.Value}");

            if (p.IterationLimit.HasValue)
                solver.SetSolverSpecificParametersAsString($"max_iterations={p.IterationLimit.Value}");
        }

        // Variables
        variables = new Variable[request.Variables.Count];
        for (int i = 0; i < request.Variables.Count; i++)
        {
            var v = request.Variables[i];
            variables[i] = v.IsInteger
                ? solver.MakeIntVar(v.LowerBound, v.UpperBound, v.Name)
                : solver.MakeNumVar(v.LowerBound, v.UpperBound, v.Name);
        }

        // Constraints
        foreach (var c in request.Constraints.Where(c => c.IsActive))
        {
            var constraint = solver.MakeConstraint(
                c.LowerBound ?? double.NegativeInfinity,
                c.UpperBound ?? double.PositiveInfinity,
                c.Name ?? string.Empty);

            for (int j = 0; j < c.Coefficients.Length; j++)
            {
                double coeff = c.Coefficients[j];
                if (Math.Abs(coeff) > 1e-9)
                    constraint.SetCoefficient(variables[j], coeff);
            }
        }

        // Combined objective
        if (request.Objectives.Count > 0)
        {
            var firstDir = request.Objectives[0].Direction ?? LinearSolverDirection.Maximize;
            var objective = solver.Objective();

            foreach (var o in request.Objectives)
            {
                for (int j = 0; j < o.Coefficients.Length; j++)
                {
                    double coeff = o.Coefficients[j] * o.Weight;
                    if (Math.Abs(coeff) > 1e-9)
                        objective.SetCoefficient(
                            variables[j],
                            objective.GetCoefficient(variables[j]) + coeff);
                }
            }

            if (firstDir == LinearSolverDirection.Maximize)
                objective.SetMaximization();
            else
                objective.SetMinimization();
        }

        return solver;
    }

    public static LinearSolverResponse Solve(LinearSolverRequest request)
    {
        var solver = BuildSolver(request, out var vars);
        var status = solver.Solve();

        var response = new LinearSolverResponse {
            Status = status.ToString(),
            ObjectiveValue = solver.Objective().Value()
        };

        // Variable results
        foreach (var v in vars)
        {
            response.Variables.Add(new LinearVariableResult {
                Name = v.Name(),
                Value = v.SolutionValue(),
                ReducedCost = v.ReducedCost()
            });
        }

        // Constraint results
        foreach (var c in solver.constraints())
        {
            double lhs = 0;
            foreach (var v in vars)
                lhs += c.GetCoefficient(v) * v.SolutionValue();

            response.Constraints.Add(new LinearConstraintResult {
                Name = c.Name(),
                Tag = request.Constraints.FirstOrDefault(x => x.Name == c.Name())?.Tag,
                LhsValue = lhs,
                // Slack = c.sla(),
                DualValue = c.DualValue()
            });
        }

        return response;
    }
}
