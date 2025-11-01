using Planner.Optimization.Tests.LinearSolver;
using System.Collections;
using System.Text.Json;

namespace Planner.Optimization.Tests;

public class LinearSolverRequestDataFromJson : IEnumerable<object[]>
{
    private readonly List<object[]> _data = new();

    public LinearSolverRequestDataFromJson()
    {
        var folder = Path.Combine(AppContext.BaseDirectory, "LinearSolver", "Cases");
        foreach (var file in Directory.GetFiles(folder, "*.json"))
        {
            var json = File.ReadAllText(file);
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var testCase = JsonSerializer.Deserialize<LinearSolverTestCase>(json, options);
            if (testCase != null)
                _data.Add([testCase]);
        }
    }

    public IEnumerator<object[]> GetEnumerator() => _data.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
