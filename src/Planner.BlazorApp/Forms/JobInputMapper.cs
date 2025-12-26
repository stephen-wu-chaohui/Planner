using Planner.BlazorApp.Forms;
using Planner.Contracts.Optimization.Inputs;

namespace Planner.Application.Optimization.Mappers;

/// <summary>
/// Maps UI job form models to solver-facing job inputs.
/// This is the only place where UI data is translated into optimization contracts.
/// </summary>
public static class JobInputMapper {

    public static JobInput ToJobInput(JobFormModel form) {
        ArgumentNullException.ThrowIfNull(form);

        return new JobInput(
            form.JobId,
            form.JobType,
            form.Name,
            new LocationInput(form.LocationId, "", form.Latitude, form.Longitude),
            form.ServiceTimeMinutes,
            form.ReadyTime,
            form.DueTime,
            form.PalletDemand,
            form.WeightDemand,
            form.RequiresRefrigeration
        );
    }

    public static IReadOnlyList<JobInput> ToJobInputs(
        IEnumerable<JobFormModel> forms) {

        ArgumentNullException.ThrowIfNull(forms);

        return forms.Select(ToJobInput).ToList();
    }
}
