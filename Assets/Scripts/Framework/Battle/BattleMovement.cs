// Assets/Scripts/Framework/Battle/BattleMovement.cs
// AgentSim — バトルグリッド上の移動範囲・行動対象計算（Unity 非依存）
//
// 向き（Facing）対応:
//   前方3方向（facing ± 1）への移動は通常コスト。
//   後方3方向への移動は battle_rear_ap_penalty の追加コストが発生する。

using System.Collections.Generic;
using AgentSim.Config;

namespace AgentSim.Battle
{
    public static class BattleMovement
    {
        // ── 向き判定ユーティリティ ─────────────────────────────────────

        /// <summary>
        /// dirIndex が facing に対して前方3方向か判定する。
        /// 前方 = 正面(diff=0)・前右(diff=1)・前左(diff=5)。
        /// </summary>
        public static bool IsFrontDirection(int facing, int dirIndex)
        {
            int diff = ((dirIndex - facing) % 6 + 6) % 6;
            return diff == 0 || diff == 1 || diff == 5;
        }

        /// <summary>from から to へのおおよその方向インデックスを返す。</summary>
        public static int GetDirectionToward(HexCoord from, HexCoord to)
        {
            var dirs = HexCoord.AllDirections;
            int dq = to.Q - from.Q;
            int dr = to.R - from.R;
            int best = 0;
            float bestScore = float.MinValue;
            for (int i = 0; i < dirs.Length; i++)
            {
                float score = dirs[i].Q * dq + dirs[i].R * dr;
                if (score > bestScore) { bestScore = score; best = i; }
            }
            return best;
        }

        // ── 移動範囲計算 ───────────────────────────────────────────────

        /// <summary>
        /// 向き対応 BFS で到達可能なマスをすべて返す（後方移動ペナルティ含む）。
        /// </summary>
        public static HashSet<HexCoord> GetReachable(BattleUnit unit, BattleGrid grid)
        {
            var (front, rear) = GetReachableSplit(unit, grid);
            var result = new HashSet<HexCoord>(front);
            result.UnionWith(rear);
            return result;
        }

        /// <summary>
        /// 前方のみで到達できるマス(front)と後方移動が必要なマス(rear)に分けて返す。
        /// front: 前方3方向のみで到達可能。
        /// rear : 後方移動を1回以上使わないと到達不可。
        /// </summary>
        public static (HashSet<HexCoord> front, HashSet<HexCoord> rear) GetReachableSplit(
            BattleUnit unit, BattleGrid grid)
        {
            float budget = unit.CurrentAp;
            var frontSet = RunBFS(unit.Position, unit.Facing, budget, grid, frontOnly: true);
            var fullSet  = RunBFS(unit.Position, unit.Facing, budget, grid, frontOnly: false);

            // rear = full に到達できるが front-only では到達できないマス
            var rearSet = new HashSet<HexCoord>(fullSet);
            rearSet.ExceptWith(frontSet);

            return (frontSet, rearSet);
        }

        // ── 内部 BFS ──────────────────────────────────────────────────
        private static HashSet<HexCoord> RunBFS(
            HexCoord startPos, int startFacing, float budget,
            BattleGrid grid, bool frontOnly)
        {
            var cfg      = SettingsRegistry.Current.Game;
            float apCost = cfg.battle_move_ap_cost;
            float rearPen = frontOnly ? float.MaxValue : cfg.battle_rear_ap_penalty;

            // state key = (HexCoord, facing)
            var visited = new Dictionary<(HexCoord, int), float>();
            var queue   = new Queue<(HexCoord pos, int facing, float spent)>();

            visited[(startPos, startFacing)] = 0f;
            queue.Enqueue((startPos, startFacing, 0f));

            while (queue.Count > 0)
            {
                var (pos, facing, spent) = queue.Dequeue();
                var dirs = HexCoord.AllDirections;

                for (int d = 0; d < dirs.Length; d++)
                {
                    bool isFront = IsFrontDirection(facing, d);
                    if (frontOnly && !isFront) continue;

                    var nb = new HexCoord(pos.Q + dirs[d].Q, pos.R + dirs[d].R);
                    if (!grid.IsInBounds(nb)) continue;
                    if (grid.IsOccupied(nb))  continue;

                    float penalty  = isFront ? 0f : rearPen;
                    float newCost  = spent + apCost + penalty;
                    if (newCost > budget + 0.001f) continue;

                    int newFacing = d;          // 移動方向が新しい向きになる
                    var key = (nb, newFacing);
                    if (visited.TryGetValue(key, out float prev) && prev <= newCost) continue;

                    visited[key] = newCost;
                    queue.Enqueue((nb, newFacing, newCost));
                }
            }

            // 出発位置を除いてマスのセットに変換
            var result = new HashSet<HexCoord>();
            foreach (var ((hex, _), _) in visited)
                if (!hex.Equals(startPos)) result.Add(hex);
            return result;
        }

        // ── アクション対象計算 ────────────────────────────────────────
        /// <summary>
        /// origin から range マス以内にいる対象ユニットのマスを返す（ユニット在籍が条件）。
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

        /// <summary>
        /// origin から range マス以内のグリッド内全マスを返す（ユニット在籍不問）。
        /// ハイライト表示用。origin 自身は除く。
        /// </summary>
        public static HashSet<HexCoord> GetActionRangeHexes(
            HexCoord origin, int range, BattleGrid grid)
        {
            var result = new HashSet<HexCoord>();
            foreach (var hex in origin.WithinRange(range))
            {
                if (hex.Equals(origin))    continue;
                if (!grid.IsInBounds(hex)) continue;
                result.Add(hex);
            }
            return result;
        }
    }
}
