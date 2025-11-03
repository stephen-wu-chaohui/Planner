using Microsoft.EntityFrameworkCore;
using Planner.Domain.Entities;
using Planner.Infrastructure.Persistence;

namespace Planner.API;

public static class APIExtension {
    public static void MapTaskEndpoints(this WebApplication app) {

        // ---- TASK ENDPOINTS ----
        var taskGroup = app.MapGroup("/api/tasks");

        taskGroup.MapGet("/", async (PlannerDbContext db) =>
            await db.Tasks.ToListAsync());

        taskGroup.MapGet("/{id:int}", async (int id, PlannerDbContext db) =>
            await db.Tasks.FindAsync(id) is TaskItem task
                ? Results.Ok(task)
                : Results.NotFound());

        taskGroup.MapPost("/", async (TaskItem task, PlannerDbContext db) => {
            db.Tasks.Add(task);
            await db.SaveChangesAsync();
            return Results.Created($"/api/tasks/{task.Id}", task);
        });

        taskGroup.MapPut("/{id:int}", async (int id, TaskItem updatedTask, PlannerDbContext db) => {
            var task = await db.Tasks.FindAsync(id);
            if (task is null) return Results.NotFound();

            task.Title = updatedTask.Title;
            task.Description = updatedTask.Description;
            task.DueDate = updatedTask.DueDate;
            task.IsCompleted = updatedTask.IsCompleted;

            await db.SaveChangesAsync();
            return Results.NoContent();
        });

        taskGroup.MapDelete("/{id:int}", async (int id, PlannerDbContext db) => {
            var task = await db.Tasks.FindAsync(id);
            if (task is null) return Results.NotFound();

            db.Tasks.Remove(task);
            await db.SaveChangesAsync();
            return Results.NoContent();
        });
    }
}
