using Planner.BlazorApp.FormModels;
using Planner.Contracts.API;

namespace Planner.BlazorApp.State.Interfaces;

// IVehicleState.cs
public interface IVehicleState : IDispatchStateProcessing {
    IReadOnlyList<VehicleDto> Vehicles { get; }
    event Action OnVehiclesChanged;
    Task SaveChangesAsync(IEnumerable<VehicleFormModel> models);
}
