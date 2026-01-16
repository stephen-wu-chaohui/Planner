using Planner.BlazorApp.State.Interfaces;

namespace Planner.BlazorApp.State;

public partial class DispatchCenterState : IDispatchStateProcessing {
    public bool IsProcessing { get; private set; }
    public string? LastErrorMessage { get; private set; }

    public event Action? OnStatusChanged;

    private void NotifyStatus() => OnStatusChanged?.Invoke();
    public void ClearError() { LastErrorMessage = null; NotifyStatus(); }

}
