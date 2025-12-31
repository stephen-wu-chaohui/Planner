using Microsoft.EntityFrameworkCore;
using Planner.Infrastructure.Persistence;

namespace Planner.API;

public static class ApiCrudExtensions {
    public static RouteGroupBuilder MapCrud<TEntity, TKey>(
        this WebApplication app,
        string route,
        Func<TEntity, TKey> keySelector)
        where TEntity : class {
        var group = app.MapGroup(route);

        // --------------------
        // GET ALL
        // --------------------
        group.MapGet("/", async (PlannerDbContext db) =>
            await db.Set<TEntity>()
                .AsNoTracking()
                .ToListAsync());

        // --------------------
        // GET BY ID
        // --------------------
        group.MapGet("/{id}", async (TKey id, PlannerDbContext db) => {
            var entity = await db.Set<TEntity>()
                .AsNoTracking()
                .FirstOrDefaultAsync(e =>
                    EqualityComparer<TKey>.Default.Equals(
                        keySelector(e), id));

            return entity is null
                ? Results.NotFound()
                : Results.Ok(entity);
        });

        // --------------------
        // CREATE
        // --------------------
        group.MapPost("/", async (TEntity entity, PlannerDbContext db) => {
            db.Set<TEntity>().Add(entity);
            await db.SaveChangesAsync();

            var id = keySelector(entity);
            return Results.Created($"{route}/{id}", entity);
        });

        // --------------------
        // UPDATE
        // --------------------
        group.MapPut("/{id}", async (TKey id, TEntity updated, PlannerDbContext db) => {
            var set = db.Set<TEntity>();

            var existing = await set.FirstOrDefaultAsync(e =>
                EqualityComparer<TKey>.Default.Equals(
                    keySelector(e), id));

            if (existing is null)
                return Results.NotFound();

            db.Entry(existing).CurrentValues.SetValues(updated);
            await db.SaveChangesAsync();

            return Results.NoContent();
        });

        // --------------------
        // DELETE
        // --------------------
        group.MapDelete("/{id}", async (TKey id, PlannerDbContext db) => {
            var set = db.Set<TEntity>();

            var entity = await set.FirstOrDefaultAsync(e =>
                EqualityComparer<TKey>.Default.Equals(
                    keySelector(e), id));

            if (entity is null)
                return Results.NotFound();

            set.Remove(entity);
            await db.SaveChangesAsync();

            return Results.NoContent();
        });

        return group;
    }
}
