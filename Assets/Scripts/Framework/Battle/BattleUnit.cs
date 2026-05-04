// Assets/Scripts/Framework/Battle/BattleUnit.cs
// AgentSim — バトル内ユニット

using System;
using AgentSim.Config;
using AgentSim.Core;

namespace AgentSim.Battle
{
    public enum BattleTeam { Player, Enemy }

    public class BattleUnit
    {
        // ── 識別 ──────────────────────────────────────────────────────
        public string     AgentId   { get; }
        public string     AgentName { get; }
        public BattleTeam Team      { get; }

        // ── ステータス参照 ────────────────────────────────────────────
        public AgentStats Stats { get; }

        // ── グリッド上の位置（BattleGrid が内部から設定） ───────────
        public HexCoord Position { get; internal set; }

        // ── HP / AP ───────────────────────────────────────────────────
        public int   CurrentHp { get; private set; }
        public int   MaxHp     { get; }
        public float CurrentAp { get; private set; }
        public float MaxAp     { get; }

        public bool IsAlive => CurrentHp > 0;

        // ── 構築 ──────────────────────────────────────────────────────
        public BattleUnit(Agent agent, BattleTeam team)
        {
            AgentId   = agent.Id;
            AgentName = agent.Name;
            Team      = team;
            Stats     = agent.Stats;

            var cfg = SettingsRegistry.Current.Game;
            MaxHp     = Stats.GetDerived(cfg.battle_hp_stat);
            CurrentHp = MaxHp;
            MaxAp     = Stats.GetDerived(cfg.battle_ap_stat);
            CurrentAp = MaxAp;
        }

        // ── HP 操作 ───────────────────────────────────────────────────
        public void TakeDamage(int amount) => CurrentHp = Math.Max(0, CurrentHp - amount);
        public void RestoreHp(int amount)  => CurrentHp = Math.Min(MaxHp, CurrentHp + amount);

        // ── AP 操作 ───────────────────────────────────────────────────
        public void SpendAp(float amount)  => CurrentAp = Math.Max(0f, CurrentAp - amount);
        public void ResetAp()              => CurrentAp = MaxAp;

        // ── ターン順用（total_power_formula を再利用） ────────────────
        public int GetSpeedStat() => Stats.TotalPower;
    }
}
