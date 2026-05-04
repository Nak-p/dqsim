// Assets/Scripts/Framework/Battle/BattleEnemyAI.cs
// AgentSim — 敵ユニットの行動決定 AI（Unity 非依存、ステートレス）

using System.Collections.Generic;
using AgentSim.Config;

namespace AgentSim.Battle
{
    public struct EnemyDecision
    {
        public HexCoord?  MoveTarget;    // null = 移動しない
        public ActionDef  Action;        // null = 行動しない
        public BattleUnit ActionTarget;
    }

    public static class BattleEnemyAI
    {
        /// <summary>
        /// 敵ユニットの今ターンの行動を決定する。
        /// 戦略: 現在位置でアクション可能なら移動せず即行動。
        ///        不可なら最近傍プレイヤーへ近づき、移動後に再評価する。
        /// </summary>
        public static EnemyDecision Decide(
            BattleUnit enemy, BattleGrid grid, List<BattleUnit> allUnits)
        {
            var players = GetLiving(allUnits, BattleTeam.Player);
            if (players.Count == 0) return default;

            var nearest  = FindNearest(enemy.Position, players);
            var decision = new EnemyDecision();

            // まず現在位置でアクション可能か評価
            var (action, target, power) = BestAction(
                enemy, enemy.Position, enemy.CurrentAp, grid, allUnits);

            if (action != null)
            {
                decision.Action       = action;
                decision.ActionTarget = target;
                return decision;
            }

            // アクション不可 → 最近傍プレイヤーへ近づく
            var reachable = BattleMovement.GetReachable(enemy, grid);
            var bestHex   = enemy.Position;
            int bestDist  = HexCoord.Distance(enemy.Position, nearest.Position);

            foreach (var hex in reachable)
            {
                int d = HexCoord.Distance(hex, nearest.Position);
                if (d < bestDist) { bestDist = d; bestHex = hex; }
            }

            if (!bestHex.Equals(enemy.Position))
                decision.MoveTarget = bestHex;

            // 移動後の位置で再評価
            var evalPos    = decision.MoveTarget ?? enemy.Position;
            float apAfterMove = decision.MoveTarget.HasValue
                ? enemy.CurrentAp - SettingsRegistry.Current.Game.battle_move_ap_cost
                : enemy.CurrentAp;

            var (action2, target2, _) = BestAction(
                enemy, evalPos, apAfterMove, grid, allUnits);

            decision.Action       = action2;
            decision.ActionTarget = target2;
            return decision;
        }

        // ── 内部ヘルパー ───────────────────────────────────────────────
        private static (ActionDef action, BattleUnit target, int power) BestAction(
            BattleUnit actor, HexCoord fromHex, float availableAp,
            BattleGrid grid, List<BattleUnit> allUnits)
        {
            ActionDef  bestAction = null;
            BattleUnit bestTarget = null;
            int        bestPower  = -1;

            var actions = SettingsRegistry.Current.Actions.actions;
            foreach (var a in actions)
            {
                if (availableAp < a.cost) continue;

                var targets = BattleMovement.GetActionTargets(
                    fromHex, a.range, grid, actor.Team, a.category);

                foreach (var hex in targets)
                {
                    var unit = grid.GetUnit(hex);
                    if (unit == null || !unit.IsAlive) continue;

                    int p = actor.Stats.GetDerived(a.primary_stat);
                    if (p > bestPower) { bestPower = p; bestAction = a; bestTarget = unit; }
                }
            }
            return (bestAction, bestTarget, bestPower);
        }

        private static List<BattleUnit> GetLiving(List<BattleUnit> all, BattleTeam team)
        {
            var result = new List<BattleUnit>();
            foreach (var u in all)
                if (u.IsAlive && u.Team == team) result.Add(u);
            return result;
        }

        private static BattleUnit FindNearest(HexCoord from, List<BattleUnit> units)
        {
            BattleUnit nearest = null;
            int minDist = int.MaxValue;
            foreach (var u in units)
            {
                int d = HexCoord.Distance(from, u.Position);
                if (d < minDist) { minDist = d; nearest = u; }
            }
            return nearest;
        }
    }
}
