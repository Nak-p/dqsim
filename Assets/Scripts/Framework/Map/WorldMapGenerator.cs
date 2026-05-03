// Assets/Scripts/Framework/Map/WorldMapGenerator.cs
// AgentSim — Perlin ノイズによるワールドマップ生成 MonoBehaviour
//
// マップサイズ・ノイズスケール・分類閾値は map_config.json から読み込む。
// タイル種別の id・色・コストは tile_types.json から読み込む。
// C# にこれらの数値・地形名をハードコーディングしてはいけない。

using System.Collections.Generic;
using UnityEngine;
using AgentSim.Config;
using AgentSim.Pathfinding;

namespace AgentSim.Map
{
    public class WorldMapGenerator : MonoBehaviour
    {
        // ── 生成済みマップ ────────────────────────────────────────────
        public MapData Map { get; private set; }

        // ── 公開 API ─────────────────────────────────────────────────
        /// <summary>
        /// SettingsRegistry の map_config と tile_types を使ってマップを生成する。
        /// 拠点〜目的地間の経路が取れるまで最大 path_retry_count 回リトライする。
        /// </summary>
        public void Generate(int seed = 0)
        {
            var cfg = SettingsRegistry.Current.MapSettings;

            for (int attempt = 0; attempt < cfg.path_retry_count; attempt++)
            {
                var map  = GenerateMap(seed + attempt * 1000, cfg);
                var path = Pathfinder.FindPath(map, map.BasePosition, map.DestinationPosition);
                if (path != null && path.Count > 1)
                {
                    Map = map;
                    Debug.Log($"[WorldMapGenerator] 生成完了 (試行 {attempt + 1} 回目): " +
                              $"拠点={map.BasePosition}, 目的地={map.DestinationPosition}, " +
                              $"経路={path.Count} タイル");
                    return;
                }
            }
            Debug.LogError("[WorldMapGenerator] マップ生成失敗: 有効な経路が見つかりません。");
        }

        // ── 内部生成ロジック ──────────────────────────────────────────
        private MapData GenerateMap(int seed, MapConfig cfg)
        {
            var map      = new MapData(cfg.width, cfg.height);
            float offX   = seed * 0.1f;
            float offY   = seed * 0.07f;

            // generator_role → tile_types インデックス のマップを事前構築
            var roleIndex = BuildRoleIndex();

            // Perlin ノイズでタイル分類
            for (int x = 0; x < cfg.width; x++)
                for (int y = 0; y < cfg.height; y++)
                {
                    float e = Mathf.PerlinNoise(x * cfg.elevation_scale + offX,
                                                 y * cfg.elevation_scale + offY);
                    float m = Mathf.PerlinNoise(x * cfg.moisture_scale  + offX + 100f,
                                                 y * cfg.moisture_scale  + offY + 100f);
                    map.Tiles[x, y] = ClassifyTile(e, m, cfg, roleIndex);
                }

            // 拠点（左下エリア）・目的地（右上エリア）を配置
            int hw = cfg.width / 2, hh = cfg.height / 2;
            map.BasePosition        = FindWalkableTile(map, roleIndex, 4, 4, hw - 1, hh - 1);
            map.DestinationPosition = FindWalkableTile(map, roleIndex, hw + 4, hh + 4,
                                                       cfg.width - 5, cfg.height - 5);

            // 離れていなければ強制配置
            if (Vector2Int.Distance(map.BasePosition, map.DestinationPosition) < cfg.min_base_dest_distance)
                map.DestinationPosition = ForceWalkableTile(map, cfg.width - 10, cfg.height - 10);

            return map;
        }

        /// <summary>
        /// generator_role 文字列 → TileTypeConfig の配列インデックス のマップを構築する。
        /// </summary>
        private static Dictionary<string, int> BuildRoleIndex()
        {
            var dict  = new Dictionary<string, int>();
            var types = SettingsRegistry.Current.TileTypes.tile_types;
            for (int i = 0; i < types.Length; i++)
            {
                var role = types[i].generator_role;
                if (!string.IsNullOrEmpty(role) && !dict.ContainsKey(role))
                    dict[role] = i;
            }
            return dict;
        }

        /// <summary>
        /// elevation/moisture 値と MapConfig 閾値からタイルインデックスを決定する。
        /// 分類ロジックは framework 固定（閾値は JSON で世界観ごとに調整可能）。
        /// </summary>
        private static int ClassifyTile(float e, float m, MapConfig cfg,
                                        Dictionary<string, int> roleIndex)
        {
            string role;
            if      (e < cfg.water_threshold)                                              role = "water";
            else if (e < cfg.forest_elevation_max  && m > cfg.forest_moisture_min)        role = "forest";
            else if (e > cfg.mountain_threshold)                                           role = "mountain";
            else if (e < cfg.desert_elevation_max  && m < cfg.desert_moisture_max)        role = "desert";
            else                                                                           role = "base";

            return roleIndex.TryGetValue(role, out int idx) ? idx : 0;
        }

        /// <summary>
        /// 指定範囲内で "base" ロールタイルを探す。なければ任意の歩行可能タイルを返す。
        /// </summary>
        private static Vector2Int FindWalkableTile(MapData map, Dictionary<string, int> roleIndex,
                                                   int minX, int minY, int maxX, int maxY)
        {
            int baseIdx = roleIndex.TryGetValue("base", out int bi) ? bi : -1;

            // まず "base" ロールのタイルを優先
            for (int y = minY; y <= maxY; y++)
                for (int x = minX; x <= maxX; x++)
                    if (map.InBounds(x, y) && map.Tiles[x, y] == baseIdx)
                        return new Vector2Int(x, y);

            // なければ任意の歩行可能タイル
            for (int y = minY; y <= maxY; y++)
                for (int x = minX; x <= maxX; x++)
                    if (map.IsWalkable(x, y))
                        return new Vector2Int(x, y);

            return new Vector2Int(Mathf.Clamp(minX, 0, map.Width - 1),
                                  Mathf.Clamp(minY, 0, map.Height - 1));
        }

        /// <summary>
        /// (startX, startY) 周辺から歩行可能タイルを探す（半径15タイル以内）。
        /// </summary>
        private static Vector2Int ForceWalkableTile(MapData map, int startX, int startY)
        {
            for (int r = 0; r < 15; r++)
                for (int dy = -r; dy <= r; dy++)
                    for (int dx = -r; dx <= r; dx++)
                    {
                        int x = Mathf.Clamp(startX + dx, 0, map.Width  - 1);
                        int y = Mathf.Clamp(startY + dy, 0, map.Height - 1);
                        if (map.IsWalkable(x, y))
                            return new Vector2Int(x, y);
                    }
            return new Vector2Int(startX, startY);
        }
    }
}
