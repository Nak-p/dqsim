using System.Collections.Generic;
using UnityEngine;

namespace DQSim
{
    public static class Pathfinder
    {
        private static readonly Vector2Int[] Directions =
        {
            Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right
        };

        public static List<Vector2Int> FindPath(MapData map, Vector2Int start, Vector2Int end)
        {
            if (!map.IsWalkable(end.x, end.y))
                return null;

            var open = new List<PathNode>();
            var closed = new HashSet<Vector2Int>();

            open.Add(new PathNode(start, 0f, Heuristic(start, end), null));

            while (open.Count > 0)
            {
                PathNode current = GetLowestF(open);
                open.Remove(current);

                if (current.Position == end)
                    return ReconstructPath(current);

                closed.Add(current.Position);

                foreach (var dir in Directions)
                {
                    var next = current.Position + dir;
                    if (!map.IsWalkable(next.x, next.y) || closed.Contains(next))
                        continue;

                    float newG = current.GCost + map.MovementCost(next.x, next.y);
                    PathNode existing = FindInOpen(open, next);

                    if (existing == null)
                    {
                        open.Add(new PathNode(next, newG, Heuristic(next, end), current));
                    }
                    else if (newG < existing.GCost)
                    {
                        existing.GCost = newG;
                        existing.Parent = current;
                    }
                }
            }

            return null;
        }

        private static float Heuristic(Vector2Int a, Vector2Int b) =>
            Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);

        private static PathNode GetLowestF(List<PathNode> open)
        {
            PathNode best = open[0];
            for (int i = 1; i < open.Count; i++)
                if (open[i].FCost < best.FCost)
                    best = open[i];
            return best;
        }

        private static PathNode FindInOpen(List<PathNode> open, Vector2Int pos)
        {
            foreach (var node in open)
                if (node.Position == pos) return node;
            return null;
        }

        private static List<Vector2Int> ReconstructPath(PathNode endNode)
        {
            var path = new List<Vector2Int>();
            PathNode current = endNode;
            while (current != null)
            {
                path.Add(current.Position);
                current = current.Parent;
            }
            path.Reverse();
            return path;
        }
    }
}
