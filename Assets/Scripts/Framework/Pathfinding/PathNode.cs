// Assets/Scripts/Framework/Pathfinding/PathNode.cs
// AgentSim — A* パスファインダーの内部ノード

using UnityEngine;

namespace AgentSim.Pathfinding
{
    internal class PathNode
    {
        public Vector2Int Position;
        public float      GCost;
        public float      HCost;
        public PathNode   Parent;

        public float FCost => GCost + HCost;

        public PathNode(Vector2Int position, float gCost, float hCost, PathNode parent)
        {
            Position = position;
            GCost    = gCost;
            HCost    = hCost;
            Parent   = parent;
        }
    }
}
