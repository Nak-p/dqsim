// Assets/Scripts/Framework/Config/TileTypeConfig.cs
// AgentSim — タイル種別定義（地形名・色・パターン・移動コスト）
//
// 世界観ごとに tile_types.json で定義する。
// C# に地形名・色・コストをハードコーディングしてはいけない。

using UnityEngine;

namespace AgentSim.Config
{
    /// <summary>
    /// 1種類のタイルの定義。tile_types.json の配列要素に対応する。
    /// </summary>
    [System.Serializable]
    public class TileTypeDef
    {
        // ── 識別 ──────────────────────────────────────────────────────
        public string id;               // "grass", "circuit_floor" など
        public string display_name;     // 表示名（"Grassland", "Circuit Floor" など）

        // ── ジェネレータ役割（マップ生成ロジックとの橋渡し） ──────────
        // "base"|"water"|"forest"|"mountain"|"desert"|"marker_base"|"marker_dest"
        // framework 内部のロール名称（世界観固有の id とは別）
        public string generator_role;

        // ── 移動コスト ────────────────────────────────────────────────
        public bool  walkable;          // false = 通行不可
        public float movement_cost;     // 1.0 = 通常, 1.5 = 遅い, 3.0 = 非常に遅い, 999 = 通行不可

        // ── 見た目 ────────────────────────────────────────────────────
        public float[] base_color;      // RGBA [0..1] float[4]
        public float[] secondary_color; // RGBA [0..1] float[4]（パターンの第二色、省略可）

        // "flat"|"checkerboard"|"dotted"|"mountain"|"marker_base"|"marker_dest"
        public string pattern;

        // ── Convenience プロパティ ─────────────────────────────────────
        /// <summary>base_color を UnityEngine.Color に変換する</summary>
        public Color BaseColor =>
            base_color != null && base_color.Length >= 3
                ? new Color(base_color[0], base_color[1], base_color[2],
                            base_color.Length > 3 ? base_color[3] : 1f)
                : Color.magenta;

        /// <summary>secondary_color を UnityEngine.Color に変換する（未設定なら BaseColor を返す）</summary>
        public Color SecondaryColor =>
            secondary_color != null && secondary_color.Length >= 3
                ? new Color(secondary_color[0], secondary_color[1], secondary_color[2],
                            secondary_color.Length > 3 ? secondary_color[3] : 1f)
                : BaseColor;
    }

    /// <summary>
    /// tile_types.json のルートオブジェクト。
    /// </summary>
    [System.Serializable]
    public class TileTypeConfig
    {
        public TileTypeDef[] tile_types;
    }
}
