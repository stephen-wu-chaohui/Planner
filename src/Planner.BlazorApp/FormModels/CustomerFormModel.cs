using System;
using Planner.Contracts.API;

namespace Planner.BlazorApp.FormModels;

/// <summary>
/// UI form model for editing customers.
/// Owns location data only at creation / edit time.
/// </summary>
public sealed class CustomerFormModel : EditableFlags {
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

    public CustomerFormModel() { }

    public CustomerFormModel(CustomerFormModel other) : base(other) {
        CustomerId = other.CustomerId;
        Name = other.Name;
        LocationId = other.LocationId;
        Address = other.Address;
        Latitude = other.Latitude;
        Longitude = other.Longitude;
        DefaultServiceMinutes = other.DefaultServiceMinutes;
        RequiresRefrigeration = other.RequiresRefrigeration;
        DefaultJobType = other.DefaultJobType;
    }
}


/// <summary>
/// Maps UI customer models to transport-safe contract models.
/// Maps contracts to UI customer models.
/// </summary>
public static class CustomerMapper {
    public static CustomerDto ToDto(this CustomerFormModel model) {
        return new CustomerDto(
            CustomerId: model.CustomerId,
            Name: model.Name,
            Location: new LocationDto(
                model.LocationId,
                model.Address,
                model.Latitude,
                model.Longitude),
            DefaultServiceMinutes: model.DefaultServiceMinutes,
            RequiresRefrigeration: model.RequiresRefrigeration
        );
    }

    public static CustomerFormModel ToFormModel(this CustomerDto dto) {
        return new CustomerFormModel {
            CustomerId = dto.CustomerId,
            Name = dto.Name,
            LocationId = dto.Location.Id,
            Address = dto.Location.Address,
            Latitude = dto.Location.Latitude,
            Longitude = dto.Location.Longitude,
            DefaultServiceMinutes = dto.DefaultServiceMinutes,
            RequiresRefrigeration = dto.RequiresRefrigeration,
            DefaultJobType = 1
        };
    }
}
