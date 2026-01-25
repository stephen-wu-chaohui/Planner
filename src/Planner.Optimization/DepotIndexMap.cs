using Planner.Messaging.Optimization.Inputs;

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
        IReadOnlyList<StopInput> locs
    ) {
        var map = new Dictionary<long, int>();
        var locIds = new List<long>();
        var nodeIds = new List<int>();

        for (int nodeIndex = 0; nodeIndex < locs.Count; nodeIndex++) {
            var l = locs[nodeIndex];
            // if (!l.IsDepot) continue;

            map.Add(l.LocationId, nodeIndex);
            locIds.Add(l.LocationId);
            nodeIds.Add(nodeIndex);
        }

        return new DepotIndexMap(locIds, nodeIds, map);
    }
}
