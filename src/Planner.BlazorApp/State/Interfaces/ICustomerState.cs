using Planner.BlazorApp.FormModels;
using Planner.Contracts.API;

namespace Planner.BlazorApp.State.Interfaces;

// ICustomerState.cs
public interface ICustomerState : IDispatchStateProcessing {
    IReadOnlyList<CustomerDto> Customers { get; }
    event Action OnCustomersChanged;
    Task SaveChangesAsync(IEnumerable<CustomerFormModel> models);
}
