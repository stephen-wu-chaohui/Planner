using Planner.Domain;
using Route = Planner.Domain.Route;
namespace Planner.API;

public static class APIExtension {
    public static void MapApiEndpoints(this WebApplication app) {
        app.MapCrud<TaskItem, long>(route: "/api/tasks", keySelector: t => t.Id);
        app.MapCrud<Vehicle, long>("/api/vehicles", v => v.Id);
        app.MapCrud<Job, long>("/api/jobs", j => j.Id);
        app.MapCrud<Location, long>("/api/locations", l => l.Id);
        app.MapCrud<Depot, long>("/api/depots", l => l.Id);
        app.MapCrud<Route, long>("/api/routes", r => r.Id);
        app.MapCrud<Customer, long>("/api/customers", c => c.CustomerId);
    }
}
