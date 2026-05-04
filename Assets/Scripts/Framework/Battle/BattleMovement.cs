// Assets/Scripts/Framework/Battle/BattleMovement.cs
// AgentSim — バトルグリッド上の移動範囲・行動対象計算（Unity 非依存）

using System.Collections.Generic;
using AgentSim.Config;

namespace AgentSim.Battle
{
    public static class BattleMovement
    {
        /// <summary>
        /// BFS でユニットの残 AP 内に到達可能なマスを返す。
        /// 占有済みマスは通行不可。出発位置は含まない。
        /// </summary>
        public static HashSet<HexCoord> GetReachable(BattleUnit unit, BattleGrid grid)
        {
            float apCost = SettingsRegistry.Current.Game.battle_move_ap_cost;
            float budget = unit.CurrentAp;

            var visited = new Dictionary<HexCoord, float>();
            var queue   = new Queue<(HexCoord hex, float spent)>();

            visited[unit.Position] = 0f;
            queue.Enqueue((unit.Position, 0f));

            while (queue.Count > 0)
            {
                var (cur, spent) = queue.Dequeue();
                foreach (var nb in cur.Neighbors())
                {
                    if (!grid.IsInBounds(nb)) continue;
                    if (grid.IsOccupied(nb))  continue;

                    float newCost = spent + apCost;
                    if (newCost > budget + 0.001f) continue;
                    if (visited.TryGetValue(nb, out float prev) && prev <= newCost) continue;

                    visited[nb] = newCost;
                    queue.Enqueue((nb, newCost));
                }
            }

            var result = new HashSet<HexCoord>(visited.Keys);
            result.Remove(unit.Position);
            return result;
        }

        /// <summary>
        /// origin から range マス以内にいる対象ユニットのマスを返す。
        /// "melee"/"ranged" → 敵ユニット、"support" → 味方ユニット。
        /// </summary>
        public static HashSet<HexCoord> GetActionTargets(
            HexCoord origin, int range, BattleGrid grid,
            BattleTeam actorTeam, string category)
        {
            bool targetEnemy = category != "support";
            BattleTeam targetTeam = targetEnemy
                ? (actorTeam == BattleTeam.Player ? BattleTeam.Enemy  : BattleTeam.Player)
                : actorTeam;

            var result = new HashSet<HexCoord>();
            foreach (var hex in origin.WithinRange(range))
            {
                if (hex.Equals(origin))    continue;
                if (!grid.IsInBounds(hex)) continue;

                var unit = grid.GetUnit(hex);
                if (unit != null && unit.IsAlive && unit.Team == targetTeam)
                    result.Add(hex);
            }
            return result;
        }
    }
}
