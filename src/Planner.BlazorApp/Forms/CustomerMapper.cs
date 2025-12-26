using Planner.BlazorApp.Forms;
using Planner.Contracts.Optimization.Inputs;

namespace Planner.BlazorApp.Mappers;

/// <summary>
/// Maps UI customer models to transport-safe contract models.
/// </summary>
public static class CustomerMapper {
    public static CustomerInput ToContract(this CustomerFormModel model) {
        return new CustomerInput(
            model.CustomerId,
            model.Name,
            new LocationInput(
                model.LocationId,
                model.Address,
                model.Latitude,
                model.Longitude
            ),
            model.DefaultServiceMinutes,
            model.RequiresRefrigeration,
            model.DefaultJobType
        );
    }

    public static IReadOnlyList<CustomerInput> ToContracts(
        IEnumerable<CustomerFormModel> models) {
        return models.Select(ToContract).ToList();
    }

}
