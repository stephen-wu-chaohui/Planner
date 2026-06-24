using MediatR;
using Microsoft.EntityFrameworkCore;
using Planner.Application.Caching;
using Planner.Application.CQRS;
using Planner.Application.Mappings;
using Planner.Application;
using Planner.Contracts.API;

namespace Planner.Application.Features.Customers;

public sealed record GetCustomersQuery : IRequest<List<CustomerDto>>;

public sealed record GetCustomerByIdQuery(long Id) : IRequest<CustomerDto?>;

public sealed record CreateCustomerCommand(CustomerDto Customer) : IRequest<CommandResult<CustomerDto>>;

public sealed record UpdateCustomerCommand(long Id, CustomerDto Customer) : IRequest<CommandResult<CustomerDto>>;

public sealed record DeleteCustomerCommand(long Id) : IRequest<CommandResult>;

public sealed class CustomerQueryHandler(IPlannerDataCenter dataCenter, ITenantContext tenant) :
    IRequestHandler<GetCustomersQuery, List<CustomerDto>>,
    IRequestHandler<GetCustomerByIdQuery, CustomerDto?> {
    public async Task<List<CustomerDto>> Handle(
        GetCustomersQuery query,
        CancellationToken cancellationToken) =>
        await dataCenter.GetOrFetchAsync(
            CacheKeys.CustomersList(tenant.TenantId),
            async () => await dataCenter.DbContext.Customers
                .AsNoTracking()
                .Where(c => c.TenantId == tenant.TenantId)
                .Include(c => c.Location)
                .Select(c => c.ToDto())
                .ToListAsync(cancellationToken),
            cancellationToken: cancellationToken) ?? [];

    public async Task<CustomerDto?> Handle(
        GetCustomerByIdQuery query,
        CancellationToken cancellationToken) =>
        await dataCenter.GetOrFetchAsync(
            CacheKeys.CustomerById(query.Id, tenant.TenantId),
            async () => await dataCenter.DbContext.Customers
                .AsNoTracking()
                .Where(c => c.TenantId == tenant.TenantId)
                .Include(c => c.Location)
                .Where(c => c.CustomerId == query.Id)
                .Select(c => c.ToDto())
                .FirstOrDefaultAsync(cancellationToken),
            cancellationToken: cancellationToken);
}

public sealed class CustomerCommandHandler(IPlannerDataCenter dataCenter, ITenantContext tenant) :
    IRequestHandler<CreateCustomerCommand, CommandResult<CustomerDto>>,
    IRequestHandler<UpdateCustomerCommand, CommandResult<CustomerDto>>,
    IRequestHandler<DeleteCustomerCommand, CommandResult> {
    public async Task<CommandResult<CustomerDto>> Handle(
        CreateCustomerCommand command,
        CancellationToken cancellationToken) {
        var entity = command.Customer.ToDomain(tenant.TenantId);
        dataCenter.DbContext.Customers.Add(entity);
        await dataCenter.DbContext.SaveChangesAsync(cancellationToken);
        await RemoveCacheAsync(entity.CustomerId, cancellationToken);

        return CommandResult<CustomerDto>.Succeeded(entity.ToDto());
    }

    public async Task<CommandResult<CustomerDto>> Handle(
        UpdateCustomerCommand command,
        CancellationToken cancellationToken) {
        if (command.Id != command.Customer.CustomerId) {
            return CommandResult<CustomerDto>.Rejected("ID mismatch");
        }

        var existing = await dataCenter.DbContext.Customers
            .Where(c => c.TenantId == tenant.TenantId)
            .FirstOrDefaultAsync(c => c.CustomerId == command.Id, cancellationToken);
        if (existing is null) {
            return CommandResult<CustomerDto>.NotFound();
        }

        var updated = command.Customer.ToDomain(tenant.TenantId);
        dataCenter.DbContext.Entry(existing).CurrentValues.SetValues(updated);
        await dataCenter.DbContext.SaveChangesAsync(cancellationToken);
        await RemoveCacheAsync(command.Id, cancellationToken);

        return CommandResult<CustomerDto>.Succeeded(command.Customer);
    }

    public async Task<CommandResult> Handle(
        DeleteCustomerCommand command,
        CancellationToken cancellationToken) {
        var entity = await dataCenter.DbContext.Customers
            .Where(c => c.TenantId == tenant.TenantId)
            .FirstOrDefaultAsync(c => c.CustomerId == command.Id, cancellationToken);
        if (entity is null) {
            return CommandResult.NotFound();
        }

        dataCenter.DbContext.Customers.Remove(entity);
        await dataCenter.DbContext.SaveChangesAsync(cancellationToken);
        await RemoveCacheAsync(command.Id, cancellationToken);

        return CommandResult.Succeeded();
    }

    private Task RemoveCacheAsync(long customerId, CancellationToken cancellationToken) =>
        dataCenter.RemoveCacheKeysAsync(
            cancellationToken,
            CacheKeys.CustomersList(tenant.TenantId),
            CacheKeys.CustomerById(customerId, tenant.TenantId));
}
