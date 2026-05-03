// Assets/Scripts/Framework/Config/BattleVisualConfig.cs
// AgentSim — バトル画面の表示設定
//
// タイル色・ユニット色はすべて JSON (battle_visual.json) から読み込む。
// C# に色定数をハードコーディングしてはいけない。

namespace AgentSim.Config
{
    [System.Serializable]
    public class BattleTileDef
    {
        public string  id;            // "floor", "player_spawn", "enemy_spawn"
        public string  display_name;  // 将来の UI 用
        public float[] color;         // RGBA [0..1]
    }

    [System.Serializable]
    public class BattleVisualConfig
    {
        public BattleTileDef[] battle_tiles;
        public float[]         player_unit_color;  // プレイヤーユニットアイコン色 RGBA
        public float[]         enemy_unit_color;   // 敵ユニットアイコン色 RGBA
    }
}
