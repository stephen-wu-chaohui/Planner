using Planner.Contracts.API;
using Planner.Domain;

namespace Planner.API.Mappings;

public static class DomainDtoMappings {
    public static LocationDto ToDto(this Location location) =>
        new(location.Id, location.Address, location.Latitude, location.Longitude);

    public static Location ToDomain(this LocationDto dto) =>
        new(dto.Id, dto.Address, dto.Latitude, dto.Longitude);

    public static DepotDto ToDto(this Depot depot) =>
        new(depot.Id, depot.Name, depot.Location.ToDto());

    public static Depot ToDomain(this DepotDto dto, Guid tenantId) =>
        new Depot {
            Id = dto.Id,
            TenantId = tenantId,
            Name = dto.Name,
            LocationId = dto.Location.Id,
            Location = dto.Location.ToDomain()
        };

    public static CustomerDto ToDto(this Customer customer) =>
        new(customer.CustomerId, customer.Name, customer.Location.ToDto(), customer.DefaultServiceMinutes, customer.RequiresRefrigeration);

    public static Customer ToDomain(this CustomerDto dto, Guid tenantId) =>
        new Customer {
            CustomerId = dto.CustomerId,
            TenantId = tenantId,
            Name = dto.Name,
            LocationId = dto.Location.Id,
            Location = dto.Location.ToDomain(),
            DefaultServiceMinutes = dto.DefaultServiceMinutes,
            RequiresRefrigeration = dto.RequiresRefrigeration
        };

    public static JobDto ToDto(this Job job) =>
        new(job.Id, job.Name, job.OrderId, job.CustomerId, job.JobType.ToDto(), job.Reference, job.Location.ToDto(), job.ServiceTimeMinutes, job.PalletDemand, job.WeightDemand, job.ReadyTime, job.DueTime, job.RequiresRefrigeration);

    public static Job ToDomain(this JobDto dto, Guid tenantId) =>
        new Job {
            Id = dto.Id,
            TenantId = tenantId,
            Name = dto.Name,
            OrderId = dto.OrderId,
            CustomerId = dto.CustomerId,
            JobType = dto.JobType.ToDomain(),
            Reference = dto.Reference,
            LocationId = dto.Location.Id,
            Location = dto.Location.ToDomain(),
            ServiceTimeMinutes = dto.ServiceTimeMinutes,
            PalletDemand = dto.PalletDemand,
            WeightDemand = dto.WeightDemand,
            ReadyTime = dto.ReadyTime,
            DueTime = dto.DueTime,
            RequiresRefrigeration = dto.RequiresRefrigeration
        };

    public static VehicleDto ToDto(this Vehicle vehicle) =>
        new(vehicle.Id, vehicle.Name, vehicle.SpeedFactor, vehicle.ShiftLimitMinutes, vehicle.DepotStartId, vehicle.DepotEndId, vehicle.DriverRatePerHour, vehicle.MaintenanceRatePerHour, vehicle.FuelRatePerKm, vehicle.BaseFee, vehicle.MaxPallets, vehicle.MaxWeight, vehicle.RefrigeratedCapacity);

    public static Vehicle ToDomain(this VehicleDto dto, Guid tenantId) =>
        new Vehicle {
            Id = dto.Id,
            TenantId = tenantId,
            Name = dto.Name,
            SpeedFactor = dto.SpeedFactor,
            ShiftLimitMinutes = dto.ShiftLimitMinutes,
            DepotStartId = dto.DepotStartId,
            DepotEndId = dto.DepotEndId,
            DriverRatePerHour = dto.DriverRatePerHour,
            MaintenanceRatePerHour = dto.MaintenanceRatePerHour,
            FuelRatePerKm = dto.FuelRatePerKm,
            BaseFee = dto.BaseFee,
            MaxPallets = dto.MaxPallets,
            MaxWeight = dto.MaxWeight,
            RefrigeratedCapacity = dto.RefrigeratedCapacity
        };

    public static JobTypeDto ToDto(this JobType jobType) => jobType switch {
        JobType.Depot => JobTypeDto.Depot,
        JobType.Pickup => JobTypeDto.Pickup,
        JobType.Delivery => JobTypeDto.Delivery,
        _ => throw new ArgumentOutOfRangeException(nameof(jobType), jobType, "Unknown job type")
    };

    public static JobType ToDomain(this JobTypeDto jobType) => jobType switch {
        JobTypeDto.Depot => JobType.Depot,
        JobTypeDto.Pickup => JobType.Pickup,
        JobTypeDto.Delivery => JobType.Delivery,
        _ => throw new ArgumentOutOfRangeException(nameof(jobType), jobType, "Unknown job type")
    };
}
