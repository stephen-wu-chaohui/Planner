using MediatR;
using Microsoft.EntityFrameworkCore;
using Planner.Application.Caching;
using Planner.Application.CQRS;
using Planner.Application.Mappings;
using Planner.Application;
using Planner.Contracts.API;

namespace Planner.Application.Features.Jobs;

public sealed record GetJobsQuery : IRequest<List<JobDto>>;

public sealed record GetJobByIdQuery(long Id) : IRequest<JobDto?>;

public sealed record CreateJobCommand(JobDto Job) : IRequest<CommandResult<JobDto>>;

public sealed record UpdateJobCommand(long Id, JobDto Job) : IRequest<CommandResult<JobDto>>;

public sealed record DeleteJobCommand(long Id) : IRequest<CommandResult>;

public sealed class JobQueryHandler(IPlannerDataCenter dataCenter, ITenantContext tenant) :
    IRequestHandler<GetJobsQuery, List<JobDto>>,
    IRequestHandler<GetJobByIdQuery, JobDto?> {
    public async Task<List<JobDto>> Handle(
        GetJobsQuery query,
        CancellationToken cancellationToken) =>
        await dataCenter.GetOrFetchAsync(
            CacheKeys.JobsList(tenant.TenantId),
            async () => await dataCenter.DbContext.Jobs
                .AsNoTracking()
                .Where(j => j.TenantId == tenant.TenantId)
                .Include(j => j.Location)
                .Select(j => j.ToDto())
                .ToListAsync(cancellationToken),
            cancellationToken: cancellationToken) ?? [];

    public async Task<JobDto?> Handle(
        GetJobByIdQuery query,
        CancellationToken cancellationToken) =>
        await dataCenter.GetOrFetchAsync(
            CacheKeys.JobById(query.Id, tenant.TenantId),
            async () => await dataCenter.DbContext.Jobs
                .AsNoTracking()
                .Where(j => j.TenantId == tenant.TenantId)
                .Include(j => j.Location)
                .Where(j => j.Id == query.Id)
                .Select(j => j.ToDto())
                .FirstOrDefaultAsync(cancellationToken),
            cancellationToken: cancellationToken);
}

public sealed class JobCommandHandler(IPlannerDataCenter dataCenter, ITenantContext tenant) :
    IRequestHandler<CreateJobCommand, CommandResult<JobDto>>,
    IRequestHandler<UpdateJobCommand, CommandResult<JobDto>>,
    IRequestHandler<DeleteJobCommand, CommandResult> {
    public async Task<CommandResult<JobDto>> Handle(
        CreateJobCommand command,
        CancellationToken cancellationToken) {
        var entity = command.Job.ToDomain(tenant.TenantId);
        dataCenter.DbContext.Jobs.Add(entity);
        await dataCenter.DbContext.SaveChangesAsync(cancellationToken);
        await RemoveCacheAsync(entity.Id, cancellationToken);

        return CommandResult<JobDto>.Succeeded(entity.ToDto());
    }

    public async Task<CommandResult<JobDto>> Handle(
        UpdateJobCommand command,
        CancellationToken cancellationToken) {
        if (command.Id != command.Job.Id) {
            return CommandResult<JobDto>.Rejected("ID mismatch");
        }

        var existing = await dataCenter.DbContext.Jobs
            .Where(j => j.TenantId == tenant.TenantId)
            .FirstOrDefaultAsync(j => j.Id == command.Id, cancellationToken);
        if (existing is null) {
            return CommandResult<JobDto>.NotFound();
        }

        var updated = command.Job.ToDomain(tenant.TenantId);
        dataCenter.DbContext.Entry(existing).CurrentValues.SetValues(updated);
        await dataCenter.DbContext.SaveChangesAsync(cancellationToken);
        await RemoveCacheAsync(command.Id, cancellationToken);

        return CommandResult<JobDto>.Succeeded(command.Job);
    }

    public async Task<CommandResult> Handle(
        DeleteJobCommand command,
        CancellationToken cancellationToken) {
        var entity = await dataCenter.DbContext.Jobs
            .Where(j => j.TenantId == tenant.TenantId)
            .FirstOrDefaultAsync(j => j.Id == command.Id, cancellationToken);
        if (entity is null) {
            return CommandResult.NotFound();
        }

        dataCenter.DbContext.Jobs.Remove(entity);
        await dataCenter.DbContext.SaveChangesAsync(cancellationToken);
        await RemoveCacheAsync(command.Id, cancellationToken);

        return CommandResult.Succeeded();
    }

    private Task RemoveCacheAsync(long jobId, CancellationToken cancellationToken) =>
        dataCenter.RemoveCacheKeysAsync(
            cancellationToken,
            CacheKeys.JobsList(tenant.TenantId),
            CacheKeys.JobById(jobId, tenant.TenantId));
}
