// Assets/Scripts/Framework/Config/ActionConfig.cs
// AgentSim — バトルアクション定義
//
// アクションは「剣攻撃」「魔法攻撃」ではなく
// 機能レベル（melee / ranged / support）で定義する。
// 表示名は display_name で世界観に合わせて JSON で上書きする。

namespace AgentSim.Config
{
    [System.Serializable]
    public class ActionDef
    {
        public string id;            // "melee_attack", "ranged_attack", "support_action"
        public string display_name;  // "Attack", "Cast", "Heal"（UI 表示用、世界観依存）
        // category: "melee" | "ranged" | "support"
        public string category;
        // このアクションに使われる derived stat の id
        // 例: "melee_power", "ranged_power", "support_power"
        public string primary_stat;
        // アクション実行に必要なアクションポイント（AP）コスト
        public float  cost;
        // 射程（タイル数）
        public int    range;
    }

    [System.Serializable]
    public class ActionConfig
    {
        public ActionDef[] actions;
    }
}
