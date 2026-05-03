// Assets/Scripts/Framework/Config/StatDefinitions.cs
// AgentSim — ステータス定義（名称 + 導出式）
//
// primary_stats : エージェントが持つ基本ステータスの id/表示名
// derived_stats : 基本ステータスの formula から計算される二次ステータス
// total_power_formula : エージェントの総合戦力計算式

namespace AgentSim.Config
{
    [System.Serializable]
    public class PrimaryStatDef
    {
        public string id;            // "strength", "vitality" など（小文字スネークケース）
        public string display_name;  // "STR", "VIT" など（UI 表示用）
        public string description;   // ツールチップ等に使う説明文
    }

    [System.Serializable]
    public class DerivedStatDef
    {
        public string id;            // "endurance", "melee_power" など
        public string display_name;  // "HP", "P.Atk" など
        // 数式文字列。使える演算子: + - * /  使えるトークン: primary stat の id
        // 例: "20 + vitality * 5"
        public string formula;
    }

    [System.Serializable]
    public class StatDefinitions
    {
        public PrimaryStatDef[] primary_stats;
        public DerivedStatDef[] derived_stats;
        // 例: "endurance / 5 + focus / 5 + melee_power + ranged_power + support_power"
        public string total_power_formula;
    }
}
