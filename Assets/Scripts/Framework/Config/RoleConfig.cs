// Assets/Scripts/Framework/Config/RoleConfig.cs
// AgentSim — ロール（職業/役割）定義
//
// DQSim の AdventurerJob (Warrior/Priest/Mage) に相当するが、
// C# の enum は持たず JSON の id 文字列で参照する。

namespace AgentSim.Config
{
    [System.Serializable]
    public class RoleDef
    {
        public string   id;            // "warrior", "priest", "heavy", "recon" など
        public string   display_name;  // "Warrior", "Heavy Combat" など（UI 表示用）

        // stat_weights[i] : primary_stats[i] への重み（生成時の配分比）
        // StatDefinitions.primary_stats と同じ順序・同じ長さであること
        public float[]  stat_weights;

        // stat_priority[i] : i 番目に優先して割り振るステータスの配列インデックス
        // 端数ゴールドの配分優先順序に使う
        public int[]    stat_priority;

        // stat_ranges[i] = [min, max] : primary_stats[i] の最低/最高値
        // Newtonsoft.Json を使うためジャグ配列が使える
        public int[][]  stat_ranges;
    }

    [System.Serializable]
    public class RoleConfig
    {
        public RoleDef[] roles;
    }
}
