// Assets/Scripts/Framework/Battle/BattleUnit.cs
// AgentSim — バトル内ユニット（Phase 1: 表示用最小版）
//
// CharacterStats を保持し、グリッド上の位置とチームを管理する。
// HP / AP 管理は Phase 2 で追加する。

using AgentSim.Core;

namespace AgentSim.Battle
{
    public enum BattleTeam { Player, Enemy }

    /// <summary>
    /// バトルグリッド上の1ユニット。
    /// Character から生成し、位置とチームを保持する。
    /// </summary>
    public class BattleUnit
    {
        // ── 識別 ──────────────────────────────────────────────────────
        public string    CharacterId   { get; }
        public string    CharacterName { get; }
        public BattleTeam Team         { get; }

        // ── ステータス参照 ────────────────────────────────────────────
        public CharacterStats Stats { get; }

        // ── グリッド上の位置（BattleGrid が内部から設定） ───────────
        public HexCoord Position { get; internal set; }

        // ── 構築 ──────────────────────────────────────────────────────
        public BattleUnit(Character character, BattleTeam team)
        {
            CharacterId   = character.Id;
            CharacterName = character.Name;
            Team          = team;
            Stats         = character.Stats;
        }
    }
}
