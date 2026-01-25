using Planner.BlazorApp.FormModels;
using Planner.Contracts.API;

namespace Planner.BlazorApp.State.Interfaces;

// IJobState.cs
public interface IJobState : IDispatchStateProcessing {
    IReadOnlyList<JobDto> Jobs { get; }
    JobDto GetJobById(long jobId);
    event Action OnJobsChanged;
    Task SaveChangesAsync(IEnumerable<JobFormModel> models);
}
