// Assets/Scripts/Framework/Config/OriginConfig.cs
// AgentSim — オリジン（出身/種族/フレームタイプ）定義
//
// DQSim の AdventurerRace (Human/Elf/Dwarf) に相当するが、
// C# の enum は持たず JSON の id 文字列で参照する。

namespace AgentSim.Config
{
    [System.Serializable]
    public class OriginDef
    {
        public string   id;            // "human", "elf", "standard_frame" など
        public string   display_name;  // "Human", "Standard Frame" など（UI 表示用）

        // stat_bonuses[i] : primary_stats[i] への加算ボーナス（正負可）
        // StatDefinitions.primary_stats と同じ順序・同じ長さであること
        public int[]    stat_bonuses;

        // 名前のプール（このオリジンのエージェント生成時にランダム選択）
        public string[] name_pool;
    }

    [System.Serializable]
    public class OriginConfig
    {
        public OriginDef[] origins;
    }
}
