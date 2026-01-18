using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;

namespace Planner.BlazorApp.Components.WelcomeWizard;

public sealed class WizardService : IDisposable {
    private readonly NavigationManager _nav;

    public bool IsOpen { get; private set; }
    public int StepIndex { get; private set; }

    public event Action? OnChange;

    public WizardService(NavigationManager nav) {
        _nav = nav;
        _nav.LocationChanged += HandleLocationChanged;
    }

    public void Open(int startStep = 0) {
        IsOpen = true;
        StepIndex = startStep;
        Notify();
    }

    public void Close() {
        if (!IsOpen) return;

        IsOpen = false;
        StepIndex = 0;
        Notify();
    }

    public void NextStep() {
        StepIndex++;
        Notify();
    }

    public void PreviousStep() {
        if (StepIndex > 0) {
            StepIndex--;
            Notify();
        }
    }

    private void HandleLocationChanged(object? sender, LocationChangedEventArgs e) {
        // CRITICAL: guarantee cleanup on navigation
        Close();
    }

    private void Notify() => OnChange?.Invoke();

    public void Dispose() {
        _nav.LocationChanged -= HandleLocationChanged;
    }
}
