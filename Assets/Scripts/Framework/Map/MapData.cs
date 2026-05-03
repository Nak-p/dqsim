// Assets/Scripts/Framework/Map/MapData.cs
// AgentSim — ワールドマップのデータコンテナ
//
// タイル種別は int インデックス（TileTypeConfig.tile_types の順序）で保持する。
// C# に TileType enum・地形名・移動コスト定数をハードコーディングしてはいけない。

using UnityEngine;
using AgentSim.Config;

namespace AgentSim.Map
{
    /// <summary>
    /// ワールドマップの2次元タイル配列と拠点・目的地の座標を保持する。
    /// WorldMapGenerator が生成し、TilemapRenderer が描画に使用する。
    /// </summary>
    public class MapData
    {
        // ── サイズ ────────────────────────────────────────────────────
        public int Width  { get; }
        public int Height { get; }

        // ── タイルデータ ──────────────────────────────────────────────
        // 各要素は TileTypeConfig.tile_types の配列インデックス
        public int[,] Tiles { get; }

        // ── 特殊座標 ──────────────────────────────────────────────────
        public Vector2Int BasePosition        { get; set; }
        public Vector2Int DestinationPosition { get; set; }

        // ── コンストラクタ ────────────────────────────────────────────
        public MapData(int width, int height)
        {
            Width  = width;
            Height = height;
            Tiles  = new int[width, height];
        }

        // ── 境界・歩行判定 ────────────────────────────────────────────
        public bool InBounds(int x, int y) =>
            x >= 0 && x < Width && y >= 0 && y < Height;

        public bool IsWalkable(int x, int y)
        {
            if (!InBounds(x, y)) return false;
            var def = GetTileDef(x, y);
            return def != null && def.walkable;
        }

        public float MovementCost(int x, int y)
        {
            if (!InBounds(x, y)) return 999f;
            var def = GetTileDef(x, y);
            return def != null ? def.movement_cost : 999f;
        }

        // ── TileTypeDef アクセサー ────────────────────────────────────
        /// <summary>座標 (x, y) の TileTypeDef を返す（範囲外または未ロードなら null）</summary>
        public TileTypeDef GetTileDef(int x, int y)
        {
            var types = SettingsRegistry.Current?.TileTypes?.tile_types;
            if (types == null) return null;
            int idx = Tiles[x, y];
            return idx >= 0 && idx < types.Length ? types[idx] : null;
        }

        // ── ユーティリティ ────────────────────────────────────────────
        /// <summary>
        /// excludeNear から minDist タイル以上離れたランダムな歩行可能座標を返す。
        /// 300 回試行しても見つからなければ DestinationPosition を返す。
        /// </summary>
        public Vector2Int GetRandomWalkableTile(System.Random rng, Vector2Int excludeNear, int minDist = 15)
        {
            for (int attempt = 0; attempt < 300; attempt++)
            {
                int x = rng.Next(2, Width - 2);
                int y = rng.Next(2, Height - 2);
                if (!IsWalkable(x, y)) continue;
                var pos = new Vector2Int(x, y);
                if (Vector2Int.Distance(pos, excludeNear) >= minDist)
                    return pos;
            }
            return DestinationPosition;
        }
    }
}
