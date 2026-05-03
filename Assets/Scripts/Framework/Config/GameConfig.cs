// Assets/Scripts/Framework/Config/GameConfig.cs
// AgentSim — 組織・通貨・用語など、世界観のトップレベル設定

namespace AgentSim.Config
{
    [System.Serializable]
    public class GameConfig
    {
        // 識別子
        public string setting_id;               // "adventurer_guild" / "robot_dispatch" など

        // 組織
        public string organization_name;        // "Guild" / "NovaTech Operations"
        public string organization_label;       // "Guild Hall" / "Operations Center"

        // エージェント（派遣する人/ユニット）の呼び方
        public string agent_term;               // "Adventurer" / "Unit"
        public string agents_term;              // "Adventurers" / "Units"

        // 案件（クエスト/ジョブ）の呼び方
        public string contract_term;            // "Quest" / "Job"
        public string contracts_term;           // "Quests" / "Jobs"

        // 通貨
        public string currency_name;            // "Gold" / "Credits"
        public string currency_symbol;          // "G" / "cr"

        // アクション用語
        public string dispatch_term;            // "Dispatch" / "Deploy"
        public string headquarters_term;        // "Guild Hall" / "Operations Center"

        // 初期パラメータ（JSON で管理、ハードコーディング禁止）
        public int    initial_currency;         // 初期通貨量
        public int    org_share_percent;        // 組織取り分 %
        public int    roster_size;              // 初期ロスター人数
        public int    initial_contracts;        // 初期案件数
        public int    max_available_contracts;  // 同時掲示最大案件数
        public float  contract_gen_interval_days; // 案件追加間隔（ゲーム内日数）
        public int    max_party_size;           // 1案件の最大パーティ人数
        public float  agent_travel_speed;       // エージェントの移動速度（タイル/ゲーム時間）

        // バトル設定
        public int    battle_grid_radius;       // ヘックスグリッド半径 (3 → 37タイル)
        public int    battle_max_turns;         // AutoResolve 上限ターン数
        public float  battle_move_ap_cost;      // 移動1マスのAPコスト
        public string battle_hp_stat;           // HPに使う derived stat id
        public string battle_ap_stat;           // APに使う derived stat id
    }
}
