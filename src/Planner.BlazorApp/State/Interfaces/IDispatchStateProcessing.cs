using Planner.Contracts.API;

namespace Planner.BlazorApp.State.Interfaces;

public interface IDispatchStateProcessing {
    string? LastErrorMessage { get; }
    bool IsProcessing { get; }

    event Action OnStatusChanged;
    void ClearError();
    Task InitializeAsync();
}
