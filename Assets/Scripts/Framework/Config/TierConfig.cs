// Assets/Scripts/Framework/Config/TierConfig.cs
// AgentSim — ティア（ランク）定義
//
// ティアはエージェントのグレードを表す。
// DQSim の Copper/Iron/Silver/Gold/Mithril に相当するが、
// C# の enum は持たず、JSON の id 文字列で参照する。

using UnityEngine;

namespace AgentSim.Config
{
    [System.Serializable]
    public class TierDef
    {
        public string   id;                  // "copper", "iron", "mark_i" など
        public string   display_name;        // "Copper", "Mark I" など（UI 表示用）
        public int      index;               // ソート/比較用の整数インデックス（0始まり）

        // バッジカラー（RGBA 0.0 〜 1.0）
        public float[]  badge_color;         // [R, G, B, A]

        // 戦力プロファイル（ガウス分布でエージェント生成に使用）
        public float    power_mean;          // 平均戦力
        public float    power_stddev;        // 標準偏差

        // ロスターの人口分布
        public float    population_weight;   // 出現確率の重み（正規化前）

        // 報酬
        public float    reward_weight;       // 報酬分配の重み乗数
        public int      contract_reward_min; // 案件報酬の最小値（このティアが要求ティアのとき）
        public int      contract_reward_max; // 案件報酬の最大値

        /// <summary>badge_color 配列から UnityEngine.Color を返す</summary>
        public Color BadgeColor =>
            badge_color != null && badge_color.Length >= 4
            ? new Color(badge_color[0], badge_color[1], badge_color[2], badge_color[3])
            : Color.white;
    }

    [System.Serializable]
    public class TierConfig
    {
        public TierDef[] tiers;
    }
}
