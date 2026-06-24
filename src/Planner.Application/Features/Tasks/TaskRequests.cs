using MediatR;
using Microsoft.EntityFrameworkCore;
using Planner.Application.Caching;
using Planner.Application.CQRS;
using Planner.Application;
using Planner.Domain;

namespace Planner.Application.Features.Tasks;

public sealed record GetTasksQuery : IRequest<List<TaskItem>>;

public sealed record GetTaskByIdQuery(long Id) : IRequest<TaskItem?>;

public sealed record CreateTaskCommand(TaskItem Task) : IRequest<CommandResult<TaskItem>>;

public sealed record UpdateTaskCommand(long Id, TaskItem Task) : IRequest<CommandResult<TaskItem>>;

public sealed record DeleteTaskCommand(long Id) : IRequest<CommandResult>;

public sealed class TaskQueryHandler(IPlannerDataCenter dataCenter, ITenantContext tenant) :
    IRequestHandler<GetTasksQuery, List<TaskItem>>,
    IRequestHandler<GetTaskByIdQuery, TaskItem?> {
    public async Task<List<TaskItem>> Handle(
        GetTasksQuery query,
        CancellationToken cancellationToken) =>
        await dataCenter.GetOrFetchAsync(
            CacheKeys.TasksList(tenant.TenantId),
            async () => await dataCenter.DbContext.Tasks
                .AsNoTracking()
                .Where(t => t.TenantId == tenant.TenantId)
                .ToListAsync(cancellationToken),
            cancellationToken: cancellationToken) ?? [];

    public async Task<TaskItem?> Handle(
        GetTaskByIdQuery query,
        CancellationToken cancellationToken) =>
        await dataCenter.GetOrFetchAsync(
            CacheKeys.TaskById(query.Id, tenant.TenantId),
            async () => await dataCenter.DbContext.Tasks
                .AsNoTracking()
                .Where(t => t.TenantId == tenant.TenantId)
                .FirstOrDefaultAsync(t => t.Id == query.Id, cancellationToken),
            cancellationToken: cancellationToken);
}

public sealed class TaskCommandHandler(IPlannerDataCenter dataCenter, ITenantContext tenant) :
    IRequestHandler<CreateTaskCommand, CommandResult<TaskItem>>,
    IRequestHandler<UpdateTaskCommand, CommandResult<TaskItem>>,
    IRequestHandler<DeleteTaskCommand, CommandResult> {
    public async Task<CommandResult<TaskItem>> Handle(
        CreateTaskCommand command,
        CancellationToken cancellationToken) {
        var entity = ForTenant(command.Task);
        dataCenter.DbContext.Tasks.Add(entity);
        await dataCenter.DbContext.SaveChangesAsync(cancellationToken);
        await RemoveCacheAsync(entity.Id, cancellationToken);

        return CommandResult<TaskItem>.Succeeded(entity);
    }

    public async Task<CommandResult<TaskItem>> Handle(
        UpdateTaskCommand command,
        CancellationToken cancellationToken) {
        if (command.Id != command.Task.Id) {
            return CommandResult<TaskItem>.Rejected("ID mismatch");
        }

        var existing = await dataCenter.DbContext.Tasks
            .Where(t => t.TenantId == tenant.TenantId)
            .FirstOrDefaultAsync(t => t.Id == command.Id, cancellationToken);
        if (existing is null) {
            return CommandResult<TaskItem>.NotFound();
        }

        var updated = ForTenant(command.Task);
        dataCenter.DbContext.Entry(existing).CurrentValues.SetValues(updated);
        await dataCenter.DbContext.SaveChangesAsync(cancellationToken);
        await RemoveCacheAsync(command.Id, cancellationToken);

        return CommandResult<TaskItem>.Succeeded(updated);
    }

    public async Task<CommandResult> Handle(
        DeleteTaskCommand command,
        CancellationToken cancellationToken) {
        var entity = await dataCenter.DbContext.Tasks
            .Where(t => t.TenantId == tenant.TenantId)
            .FirstOrDefaultAsync(t => t.Id == command.Id, cancellationToken);
        if (entity is null) {
            return CommandResult.NotFound();
        }

        dataCenter.DbContext.Tasks.Remove(entity);
        await dataCenter.DbContext.SaveChangesAsync(cancellationToken);
        await RemoveCacheAsync(command.Id, cancellationToken);

        return CommandResult.Succeeded();
    }

    private TaskItem ForTenant(TaskItem task) =>
        new() {
            Id = task.Id,
            TenantId = tenant.TenantId,
            Title = task.Title,
            Description = task.Description,
            DueDate = task.DueDate,
            IsCompleted = task.IsCompleted
        };

    private Task RemoveCacheAsync(long taskId, CancellationToken cancellationToken) =>
        dataCenter.RemoveCacheKeysAsync(
            cancellationToken,
            CacheKeys.TasksList(tenant.TenantId),
            CacheKeys.TaskById(taskId, tenant.TenantId));
}
