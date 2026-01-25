namespace Planner.Optimization;

internal sealed class DepotIndexMap {
    private readonly Dictionary<long, int> _byLocationId;

    public IReadOnlyList<long> DepotLocationIds { get; }
    public IReadOnlyList<int> DepotNodeIndices { get; }

    private DepotIndexMap(
        IReadOnlyList<long> locationIds,
        IReadOnlyList<int> nodeIndices,
        Dictionary<long, int> map
    ) {
        DepotLocationIds = locationIds;
        DepotNodeIndices = nodeIndices;
        _byLocationId = map;
    }

    public int NodeIndexOf(long depotLocationId) {
        if (!_byLocationId.TryGetValue(depotLocationId, out var idx))
            throw new SolverInputInvalidException(
                $"Depot LocationId {depotLocationId} not found in solver graph."
            );
        return idx;
    }

    public static DepotIndexMap FromSolverLocations(
        IReadOnlyList<VehicleRoutingProblem.SolverLocation> locs
    ) {
        var map = new Dictionary<long, int>();
        var locIds = new List<long>();
        var nodeIds = new List<int>();

        foreach (var l in locs) {
            // if (!l.IsDepot) continue;

            map.Add(l.LocationId, l.NodeIndex);
            locIds.Add(l.LocationId);
            nodeIds.Add(l.NodeIndex);
        }

        return new DepotIndexMap(locIds, nodeIds, map);
    }
}
