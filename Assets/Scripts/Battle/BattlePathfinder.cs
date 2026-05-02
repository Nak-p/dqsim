using System.Collections.Generic;
using UnityEngine;

namespace DQSim.Battle
{
    public static class BattlePathfinder
    {
        public struct ReachableTile
        {
            public Vector2Int Position;
            public float CostToReach;
            public Vector2Int? Parent;
        }

        /// <summary>
        /// Dijkstra's algorithm to find all tiles reachable from start within maxCost.
        /// </summary>
        public static Dictionary<Vector2Int, ReachableTile> GetReachableTiles(BattleHexMap map, Vector2Int start, float maxCost)
        {
            var reachable = new Dictionary<Vector2Int, ReachableTile>();
            var priorityQueue = new List<ReachableTile>();

            priorityQueue.Add(new ReachableTile { Position = start, CostToReach = 0, Parent = null });

            var neighborBuffer = new List<Vector2Int>(6);

            while (priorityQueue.Count > 0)
            {
                // Simple list-based priority queue for simplicity in this test
                int bestIdx = 0;
                for (int i = 1; i < priorityQueue.Count; i++)
                {
                    if (priorityQueue[i].CostToReach < priorityQueue[bestIdx].CostToReach)
                        bestIdx = i;
                }

                var current = priorityQueue[bestIdx];
                priorityQueue.RemoveAt(bestIdx);

                if (reachable.ContainsKey(current.Position))
                    continue;

                reachable[current.Position] = current;

                HexCoordinates.GetNeighbors(map, current.Position.x, current.Position.y, neighborBuffer);
                foreach (var neighbor in neighborBuffer)
                {
                    if (reachable.ContainsKey(neighbor))
                        continue;

                    float moveCost = map.MovementCost(neighbor.x, neighbor.y);
                    float newTotalCost = current.CostToReach + moveCost;

                    if (newTotalCost <= maxCost)
                    {
                        priorityQueue.Add(new ReachableTile
                        {
                            Position = neighbor,
                            CostToReach = newTotalCost,
                            Parent = current.Position
                        });
                    }
                }
            }

            return reachable;
        }

        public static List<Vector2Int> ConstructPath(Dictionary<Vector2Int, ReachableTile> reachable, Vector2Int end)
        {
            if (!reachable.ContainsKey(end)) return null;

            var path = new List<Vector2Int>();
            var current = end;
            while (true)
            {
                path.Add(current);
                var node = reachable[current];
                if (node.Parent == null) break;
                current = node.Parent.Value;
            }
            path.Reverse();
            return path;
        }
    }
}
