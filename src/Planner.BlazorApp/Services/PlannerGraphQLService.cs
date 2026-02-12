using Planner.Contracts.API;

namespace Planner.BlazorApp.Services;

public sealed class PlannerGraphQLService(GraphQLClient graphQLClient) {
    private const string VehiclesQuery = """
        query GetVehicles {
          vehicles {
            id
            name
            speedFactor
            shiftLimitMinutes
            depotStartId
            depotEndId
            driverRatePerHour
            maintenanceRatePerHour
            fuelRatePerKm
            baseFee
            maxPallets
            maxWeight
            refrigeratedCapacity
          }
        }
        """;

    private const string CustomersQuery = """
        query GetCustomers {
          customers {
            customerId
            name
            location {
              id
              address
              latitude
              longitude
            }
            defaultServiceMinutes
            requiresRefrigeration
          }
        }
        """;

    private const string JobsQuery = """
        query GetJobs {
          jobs {
            id
            name
            orderId
            customerId
            jobType
            reference
            location {
              id
              address
              latitude
              longitude
            }
            serviceTimeMinutes
            palletDemand
            weightDemand
            readyTime
            dueTime
            requiresRefrigeration
          }
        }
        """;

    public Task<List<VehicleDto>> GetVehiclesAsync(CancellationToken cancellationToken = default) =>
        ExecuteAndExtractAsync<VehiclesData, VehicleDto>(VehiclesQuery, data => data.Vehicles, cancellationToken);

    public Task<List<CustomerDto>> GetCustomersAsync(CancellationToken cancellationToken = default) =>
        ExecuteAndExtractAsync<CustomersData, CustomerDto>(CustomersQuery, data => data.Customers, cancellationToken);

    public Task<List<JobDto>> GetJobsAsync(CancellationToken cancellationToken = default) =>
        ExecuteAndExtractAsync<JobsData, JobDto>(JobsQuery, data => data.Jobs, cancellationToken);

    private async Task<List<TItem>> ExecuteAndExtractAsync<TData, TItem>(
        string query,
        Func<TData, List<TItem>?> selector,
        CancellationToken cancellationToken)
        where TData : class, new() {
        var response = await graphQLClient.ExecuteAsync<TData>(query, cancellationToken: cancellationToken);
        if (response is null) {
            return [];
        }

        if (response.Errors is { Length: > 0 }) {
            var message = string.Join("; ", response.Errors.Select(e => e.Message));
            throw new InvalidOperationException($"GraphQL request failed: {message}");
        }

        if (response.Data is null) {
            return [];
        }

        return selector(response.Data) ?? [];
    }

    private sealed class VehiclesData {
        public List<VehicleDto>? Vehicles { get; init; }
    }

    private sealed class CustomersData {
        public List<CustomerDto>? Customers { get; init; }
    }

    private sealed class JobsData {
        public List<JobDto>? Jobs { get; init; }
    }
}
