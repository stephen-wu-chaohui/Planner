using Planner.Domain;

namespace Planner.BlazorApp.Forms;

/// <summary>
/// UI form model for editing customers.
/// Owns location data only at creation / edit time.
/// </summary>
public sealed record CustomerFormModel {
    /// <summary>
    /// Tenant-scoped customer identifier.
    /// </summary>
    public long CustomerId { get; init; } = 0;

    public string Name { get; set; } = string.Empty;

    // Location (immutable once persisted, but editable in UI form)
    public long LocationId { get; init; } = 0;

    public string Address { get; set; } = string.Empty;

    public double Latitude { get; init; }

    public double Longitude { get; init; }

    public long DefaultServiceMinutes { get; set; } = 30;

    public bool RequiresRefrigeration { get; set; } = false;

    /// <summary>
    /// Default job type created from this customer.
    /// 0 = Depot, 1 = Delivery, 2 = Pickup
    /// </summary>
    public int DefaultJobType { get; set; } = 1;
}


/// <summary>
/// Maps UI customer models to transport-safe contract models.  Use Domain.Customer as the contract here.
/// Maps contracts to UI customer models.  Use Domain.Customer as the contract here.
/// </summary>
public static class CustomerMapper {
    public static Customer ToContract(this CustomerFormModel model) {
        return new Customer {
            CustomerId = model.CustomerId,
            Name = model.Name,
            Location = new Location(
                model.LocationId,
                model.Address,
                model.Latitude,
                model.Longitude
            ),
            DefaultServiceMinutes = model.DefaultServiceMinutes,
            RequiresRefrigeration = model.RequiresRefrigeration,
        };
    }

    public static CustomerFormModel ToFormModel(this Customer customer) {
        return new CustomerFormModel {
            CustomerId = customer.CustomerId,
            Name = customer.Name,
            LocationId = customer.Location.Id,
            Address = customer.Location.Address,
            Latitude = customer.Location.Latitude,
            Longitude = customer.Location.Longitude,
            DefaultServiceMinutes = customer.DefaultServiceMinutes,
            RequiresRefrigeration = customer.RequiresRefrigeration
        };
    }
}
