// Assets/Scripts/Framework/Config/MapConfig.cs
// AgentSim — ワールドマップ生成パラメータ
//
// マップサイズ・Perlin ノイズスケール・タイル分類閾値はすべて JSON から読む。
// C# にこれらの数値をハードコーディングしてはいけない。

namespace AgentSim.Config
{
    /// <summary>
    /// map_config.json のルートオブジェクト。
    /// WorldMapGenerator がマップ生成時に参照する。
    /// </summary>
    [System.Serializable]
    public class MapConfig
    {
        // ── マップサイズ ───────────────────────────────────────────────
        public int width;                       // タイル横幅（例: 64）
        public int height;                      // タイル縦幅（例: 40）

        // ── Perlin ノイズスケール ──────────────────────────────────────
        public float elevation_scale;           // 高度ノイズのスケール（例: 0.05）
        public float moisture_scale;            // 湿度ノイズのスケール（例: 0.08）

        // ── タイル分類閾値 ─────────────────────────────────────────────
        // elevation e, moisture m ∈ [0, 1]
        public float water_threshold;           // e < この値 → water ロール
        public float forest_elevation_max;      // e < この値 かつ m > forest_moisture_min → forest ロール
        public float forest_moisture_min;
        public float mountain_threshold;        // e > この値 → mountain ロール
        public float desert_elevation_max;      // e < この値 かつ m < desert_moisture_max → desert ロール
        public float desert_moisture_max;
        // 上記いずれにも該当しない場合 → base ロール

        // ── 配置制約 ──────────────────────────────────────────────────
        public int min_base_dest_distance;      // 拠点〜目的地の最小距離（タイル数）
        public int path_retry_count;            // 経路が取れない場合のリトライ回数
    }
}
