using UnityEngine;

namespace DQSim.Battle
{
    /// <summary>
    /// 六角グリッド上の地形データ。セル座標は Unity Tilemap と同一:
    /// x = 列 (0 .. Width-1), y = 行 (0 .. Height-1), z = 0。
    /// Unity の「Hexagonal Point Top」では奇数行がオフセットされる（描画は Grid に任せる）。
    /// </summary>
    public sealed class BattleHexMap
    {
        public int Width { get; }
        public int Height { get; }

        private readonly BattleTerrain[,] _tiles;

        public BattleHexMap(int width, int height)
        {
            Width = width;
            Height = height;
            _tiles = new BattleTerrain[width, height];
        }

        public BattleTerrain Get(int x, int y) => _tiles[x, y];

        public void Set(int x, int y, BattleTerrain terrain) => _tiles[x, y] = terrain;

        public bool InBounds(int x, int y) => x >= 0 && x < Width && y >= 0 && y < Height;

        public bool IsWalkable(int x, int y) =>
            InBounds(x, y) && BattleTerrainCost.IsWalkable(_tiles[x, y]);

        public float MovementCost(int x, int y) =>
            InBounds(x, y) ? BattleTerrainCost.Get(_tiles[x, y]) : 999f;
    }
}
