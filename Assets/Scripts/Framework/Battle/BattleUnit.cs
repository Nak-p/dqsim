// Assets/Scripts/Framework/Battle/BattleUnit.cs
// AgentSim — バトル内ユニット（Phase 1: 表示用最小版）
//
// AgentStats を保持し、グリッド上の位置とチームを管理する。
// HP / AP 管理は Phase 2 で追加する。

using AgentSim.Core;

namespace AgentSim.Battle
{
    public enum BattleTeam { Player, Enemy }

    /// <summary>
    /// バトルグリッド上の1ユニット。
    /// Agent から生成し、位置とチームを保持する。
    /// </summary>
    public class BattleUnit
    {
        // ── 識別 ────────────────────────────────────────���─────────────
        public string    AgentId   { get; }
        public string    AgentName { get; }
        public BattleTeam Team     { get; }

        // ── ステータス参照 ────────────────────────────────────────────
        public AgentStats Stats { get; }

        // ── グリッド上の位置（BattleGrid が内部から設定） ───────────
        public HexCoord Position { get; internal set; }

        // ── 構築 ──────────────────────────────────────────────────────
        public BattleUnit(Agent agent, BattleTeam team)
        {
            AgentId   = agent.Id;
            AgentName = agent.Name;
            Team      = team;
            Stats     = agent.Stats;
        }
    }
}
