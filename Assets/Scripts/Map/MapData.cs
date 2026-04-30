using UnityEngine;
using DQSim;

namespace DQSim
{
    public class MapData
    {
        public const int Width = 64;
        public const int Height = 40;

        public TileType[,] Tiles { get; } = new TileType[Width, Height];
        public Vector2Int BasePosition { get; set; }
        public Vector2Int DestinationPosition { get; set; }

        public bool InBounds(int x, int y) => x >= 0 && x < Width && y >= 0 && y < Height;

        public bool IsWalkable(int x, int y) =>
            InBounds(x, y) && TileMovementCost.IsWalkable(Tiles[x, y]);

        public float MovementCost(int x, int y) =>
            InBounds(x, y) ? TileMovementCost.Get(Tiles[x, y]) : 999f;

        public Vector2Int GetRandomQuestTile(System.Random rng, Vector2Int excludeNear, int minDist = 15)
        {
            for (int attempt = 0; attempt < 300; attempt++)
            {
                int x = rng.Next(2, Width - 2);
                int y = rng.Next(2, Height - 2);
                if (!IsWalkable(x, y)) continue;
                var pos = new Vector2Int(x, y);
                if (UnityEngine.Vector2Int.Distance(pos, excludeNear) >= minDist)
                    return pos;
            }
            return DestinationPosition;
        }
    }
}
