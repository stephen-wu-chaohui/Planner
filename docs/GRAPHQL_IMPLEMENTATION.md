# GraphQL Implementation in Planner

This document describes the GraphQL implementation in the Planner application.

## Overview

GraphQL has been implemented in the Planner.API project using [HotChocolate](https://chillicream.com/docs/hotchocolate/v13), a powerful .NET GraphQL server. The API exposes a GraphQL endpoint at `/graphql` that supports queries and mutations for all major entities in the system.

## API Endpoint

- **GraphQL Endpoint**: `https://your-api-url/graphql`
- **GraphiQL UI**: Available in development mode at the same endpoint for interactive exploration

## Available Operations

### Queries

All query operations support fetching individual items by ID or lists of items.

#### Jobs
```graphql
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

query GetJobById {
  jobById(id: 1) {
    id
    name
    location {
      address
    }
  }
}
```

#### Customers
```graphql
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

query GetCustomerById {
  customerById(id: 1) {
    customerId
    name
    location {
      address
    }
  }
}
```

#### Vehicles
```graphql
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

query GetVehicleById {
  vehicleById(id: 1) {
    id
    name
    maxPallets
  }
}
```

#### Depots
```graphql
query GetDepots {
  depots {
    id
    name
    location {
      id
      address
      latitude
      longitude
    }
  }
}

query GetDepotById {
  depotById(id: 1) {
    id
    name
    location {
      address
    }
  }
}
```

#### Locations
```graphql
query GetLocations {
  locations {
    id
    address
    latitude
    longitude
  }
}

query GetLocationById {
  locationById(id: 1) {
    id
    address
    latitude
    longitude
  }
}
```

#### Routes
```graphql
query GetRoutes {
  routes {
    id
    tenantId
  }
}

query GetRouteById {
  routeById(id: 1) {
    id
    tenantId
  }
}
```

#### Tasks
```graphql
query GetTasks {
  tasks {
    id
    tenantId
    title
    description
    dueDate
    isCompleted
  }
}

query GetTaskById {
  taskById(id: 1) {
    id
    title
    isCompleted
  }
}
```

### Mutations

All mutations require authentication via JWT token in the Authorization header.

#### Jobs
```graphql
mutation CreateJob {
  createJob(input: {
    id: 0
    name: "Delivery 123"
    orderId: 1
    customerId: 1
    jobType: DELIVERY
    reference: "REF-123"
    location: {
      id: 1
      address: "123 Main St"
      latitude: -31.95
      longitude: 115.86
    }
    serviceTimeMinutes: 15
    palletDemand: 5
    weightDemand: 500
    readyTime: 0
    dueTime: 1440
    requiresRefrigeration: false
  }) {
    id
    name
  }
}

mutation UpdateJob {
  updateJob(id: 1, input: {
    id: 1
    name: "Updated Job"
    # ... other fields
  }) {
    id
    name
  }
}

mutation DeleteJob {
  deleteJob(id: 1)
}
```

#### Customers
```graphql
mutation CreateCustomer {
  createCustomer(input: {
    customerId: 0
    name: "Acme Corp"
    location: {
      id: 0
      address: "456 Business Ave"
      latitude: -31.96
      longitude: 115.87
    }
    defaultServiceMinutes: 30
    requiresRefrigeration: false
  }) {
    customerId
    name
  }
}

mutation UpdateCustomer {
  updateCustomer(id: 1, input: {
    customerId: 1
    name: "Updated Customer"
    # ... other fields
  }) {
    customerId
    name
  }
}

mutation DeleteCustomer {
  deleteCustomer(id: 1)
}
```

#### Vehicles
```graphql
mutation CreateVehicle {
  createVehicle(input: {
    id: 0
    name: "Truck 01"
    speedFactor: 1.0
    shiftLimitMinutes: 480
    depotStartId: 1
    depotEndId: 1
    driverRatePerHour: 25.0
    maintenanceRatePerHour: 5.0
    fuelRatePerKm: 0.5
    baseFee: 50.0
    maxPallets: 20
    maxWeight: 5000
    refrigeratedCapacity: 0
  }) {
    id
    name
  }
}

mutation UpdateVehicle {
  updateVehicle(id: 1, input: {
    id: 1
    name: "Updated Vehicle"
    # ... other fields
  }) {
    id
    name
  }
}

mutation DeleteVehicle {
  deleteVehicle(id: 1)
}
```

## BlazorApp GraphQL Client

A simple GraphQL client has been created in the BlazorApp for consuming the GraphQL API.

### Using the GraphQL Client

```csharp
using Planner.BlazorApp.Services;

public class MyService {
    private readonly GraphQLClient _graphqlClient;
    
    public MyService(GraphQLClient graphqlClient) {
        _graphqlClient = graphqlClient;
    }
    
    public async Task<List<JobDto>?> GetJobsAsync() {
        const string query = @"
            query {
                jobs {
                    id
                    name
                    location {
                        address
                    }
                }
            }
        ";
        
        var response = await _graphqlClient.ExecuteAsync<JobsResponse>(query);
        return response?.Data?.Jobs;
    }
    
    public async Task<JobDto?> CreateJobAsync(JobDto input) {
        const string mutation = @"
            mutation CreateJob($input: JobDtoInput!) {
                createJob(input: $input) {
                    id
                    name
                }
            }
        ";
        
        var variables = new { input };
        var response = await _graphqlClient.ExecuteAsync<CreateJobResponse>(
            mutation, 
            variables
        );
        return response?.Data?.CreateJob;
    }
}

public class JobsResponse {
    public List<JobDto>? Jobs { get; set; }
}

public class CreateJobResponse {
    public JobDto? CreateJob { get; set; }
}
```

### Authentication

The GraphQL client automatically includes the JWT token from the `IJwtTokenStore` in the Authorization header:

```
Authorization: Bearer <your-jwt-token>
```

If the token is expired or invalid, the client will clear the token store and return null.

## Migrating from REST to GraphQL

### Current State

The BlazorApp currently uses REST API endpoints through the `PlannerApiClient` class. Example:

```csharp
// REST API call
var jobs = await api.GetFromJsonAsync<List<JobDto>>("api/jobs");
```

### GraphQL Migration Pattern

To migrate to GraphQL, replace REST calls with GraphQL queries:

```csharp
// GraphQL API call
const string query = @"
    query {
        jobs {
            id
            name
            location { address }
        }
    }
";
var response = await graphqlClient.ExecuteAsync<JobsResponse>(query);
var jobs = response?.Data?.Jobs ?? new List<JobDto>();
```

### Benefits of GraphQL

1. **Flexible Data Fetching**: Request only the fields you need
2. **Reduced Over-fetching**: No more receiving unnecessary data
3. **Single Endpoint**: All operations through `/graphql`
4. **Strong Typing**: GraphQL schema provides type safety
5. **Better Performance**: Fetch multiple resources in a single request

### Example: Updating DispatchCenterState

Here's how to update the Job state to use GraphQL:

```csharp
// Before (REST)
public async Task SaveChangesAsync(IEnumerable<JobFormModel> models) {
    // ... validation logic
    _jobs = await api.GetFromJsonAsync<List<JobDto>>("api/jobs") ?? [];
}

// After (GraphQL)
public async Task SaveChangesAsync(IEnumerable<JobFormModel> models) {
    // ... validation logic
    const string query = @"
        query {
            jobs {
                id
                name
                orderId
                customerId
                jobType
                reference
                location { id address latitude longitude }
                serviceTimeMinutes
                palletDemand
                weightDemand
                readyTime
                dueTime
                requiresRefrigeration
            }
        }
    ";
    var response = await graphqlClient.ExecuteAsync<JobsResponse>(query);
    _jobs = response?.Data?.Jobs ?? [];
}
```

## Testing GraphQL API

### Using GraphiQL (Development)

In development mode, navigate to `https://localhost:7085/graphql` in your browser to access the GraphiQL interface for interactive testing.

### Using cURL

```bash
# Query example
curl -X POST https://your-api-url/graphql \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -d '{"query":"{ jobs { id name } }"}'

# Mutation example
curl -X POST https://your-api-url/graphql \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -d '{
    "query": "mutation CreateJob($input: JobDtoInput!) { createJob(input: $input) { id name } }",
    "variables": {
      "input": {
        "id": 0,
        "name": "New Job",
        ...
      }
    }
  }'
```

### Using Postman

1. Set request method to POST
2. Set URL to `https://your-api-url/graphql`
3. Add Authorization header with Bearer token
4. In Body, select "GraphQL" and enter your query/mutation

## Architecture

### API Layer (`Planner.API`)

- **GraphQL/Query.cs**: Contains all query resolvers
- **GraphQL/Mutation.cs**: Contains all mutation resolvers
- **Program.cs**: Configures GraphQL server with HotChocolate

### BlazorApp Layer (`Planner.BlazorApp`)

- **Services/GraphQLClient.cs**: Simple HTTP-based GraphQL client
- **Services/PlannerApiClient.cs**: Existing REST client (can be gradually migrated)

## Future Enhancements

1. **Subscriptions**: Add real-time updates using GraphQL subscriptions
2. **DataLoaders**: Implement batching and caching for better performance
3. **Filtering & Sorting**: Add support for dynamic filtering and sorting
4. **Pagination**: Implement cursor-based pagination for large datasets
5. **Code Generation**: Use StrawberryShake for type-safe client generation

## Troubleshooting

### Common Issues

1. **401 Unauthorized**: Ensure JWT token is valid and included in Authorization header
2. **Namespace Conflicts**: `Location` type conflicts with HotChocolate.Location - use fully qualified names
3. **Type Mismatches**: Ensure DTOs match between client and server

### Debugging

Enable detailed error messages in development:

```csharp
// In Program.cs
if (app.Environment.IsDevelopment()) {
    app.UseDeveloperExceptionPage();
}
```

## References

- [HotChocolate Documentation](https://chillicream.com/docs/hotchocolate/v13)
- [GraphQL Specification](https://spec.graphql.org/)
- [GraphQL Best Practices](https://graphql.org/learn/best-practices/)
