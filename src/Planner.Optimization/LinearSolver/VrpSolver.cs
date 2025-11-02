using Google.OrTools.ConstraintSolver;
using Planner.Contracts.Messages.VehicleRoutingProblem;


namespace Planner.Optimization.Solvers;

public class VrpSolver
{
    public static VrpResult Solve(VrpRequest request)
    {
        int numJobs = request.Jobs.Count;
        int numVehicles = request.Vehicles.Count;
        int depotIndex = 0;

        // Include depot + jobs in distance matrix
        int size = numJobs + 1;
        var distanceMatrix = request.DistanceMatrix;

        // Manager: handles indexing between model and data
        var manager = new RoutingIndexManager(size, numVehicles, depotIndex);
        var model = new RoutingModel(manager);

        // Distance callback
        int transitCallbackIndex = model.RegisterTransitCallback((long fromIndex, long toIndex) => {
            int fromNode = manager.IndexToNode(fromIndex);
            int toNode = manager.IndexToNode(toIndex);
            return (long)(distanceMatrix[fromNode][toNode] * 1000); // convert km→m for precision
        });

        model.SetArcCostEvaluatorOfAllVehicles(transitCallbackIndex);

        // First solution strategy
        var searchParameters = operations_research_constraint_solver.DefaultRoutingSearchParameters();
        searchParameters.FirstSolutionStrategy = FirstSolutionStrategy.Types.Value.PathCheapestArc;
        searchParameters.TimeLimit = new Google.Protobuf.WellKnownTypes.Duration { Seconds = 5 };

        // Solve
        var solution = model.SolveWithParameters(searchParameters);

        var result = new VrpResult();

        if (solution != null)
        {
            for (int v = 0; v < numVehicles; v++)
            {
                var route = new VehicleRoute { VehicleId = request.Vehicles[v].Id };
                double routeDistance = 0;
                var index = model.Start(v);
                while (!model.IsEnd(index))
                {
                    int nodeIndex = manager.IndexToNode(index);
                    if (nodeIndex > 0) // skip depot
                    {
                        var job = request.Jobs[nodeIndex - 1];
                        route.Stops.Add(new RouteStop {
                            JobId = job.Id,
                            Latitude = job.Latitude,
                            Longitude = job.Longitude
                        });
                    }
                    var prevIndex = index;
                    index = solution.Value(model.NextVar(index));
                    routeDistance += model.GetArcCostForVehicle(prevIndex, index, v) / 1000.0;
                }

                // add depot start/end
                route.Stops.Insert(0, new RouteStop {
                    JobId = request.Depot.Id,
                    Latitude = request.Depot.Latitude,
                    Longitude = request.Depot.Longitude
                });
                route.Stops.Add(new RouteStop {
                    JobId = request.Depot.Id,
                    Latitude = request.Depot.Latitude,
                    Longitude = request.Depot.Longitude
                });

                route.RouteDistance = routeDistance;
                result.Vehicles.Add(route);
            }

            result.TotalDistance = result.Vehicles.Sum(r => r.RouteDistance);
            result.ObjectiveValue = result.TotalDistance;
            result.SolverStatus = "Success";
        } else
        {
            result.SolverStatus = "No Solution Found";
        }

        return result;
    }
}
