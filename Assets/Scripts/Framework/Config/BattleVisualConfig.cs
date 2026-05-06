// Assets/Scripts/Framework/Config/BattleVisualConfig.cs
// AgentSim — バトル画面の表示設定
//
// タイル色・ユニット色・ハイライト色はすべて JSON (battle_visual.json) から読み込む。
// C# に色定数をハードコーディングしてはいけない。

namespace AgentSim.Config
{
    [System.Serializable]
    public class BattleTileDef
    {
        public string  id;            // "floor", "player_spawn", "enemy_spawn"
        public string  display_name;
        public float[] color;         // RGBA [0..1]
    }

    [System.Serializable]
    public class BattleVisualConfig
    {
        public BattleTileDef[] battle_tiles;
        public float[]         player_unit_color;      // プレイヤーユニットアイコン色 RGBA
        public float[]         enemy_unit_color;        // 敵ユニットアイコン色 RGBA
        public float[]         highlight_move_color;    // 移動可能マスのオーバーレイ色
        public float[]         highlight_attack_color;  // 攻撃対象マスのオーバーレイ色
        public float[]         highlight_active_color;   // アクティブユニット位置の色
        public float[]         highlight_rear_move_color; // 後方移動マスの色
        public float[]         highlight_facing_color;    // 向き表示アークの色
    }
}

