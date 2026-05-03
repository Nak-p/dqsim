// Assets/Scripts/Framework/Battle/BattleGrid.cs
// AgentSim — ヘックスバトルグリッドの状態管理
//
// 有効タイルセットとユニット配置を保持する。
// Unity 非依存（System のみ）。

using System;
using System.Collections.Generic;

namespace AgentSim.Battle
{
    public class BattleGrid
    {
        // ── 内部状態 ───────────────────────────────────────���──────────
        private readonly HashSet<HexCoord>                _validHexes;
        private readonly Dictionary<HexCoord, BattleUnit> _occupants;

        public int Radius { get; }

        // ── 構築 ────────────────────────────────────────────��─────────
        public BattleGrid(int radius)
        {
            Radius      = radius;
            _validHexes = new HashSet<HexCoord>();
            _occupants  = new Dictionary<HexCoord, BattleUnit>();

            var center = new HexCoord(0, 0);
            foreach (var hex in center.WithinRange(radius))
                _validHexes.Add(hex);
        }

        // ── 範囲チェック ────────────────────────��─────────────────────
        public bool IsInBounds(HexCoord hex) => _validHexes.Contains(hex);

        public IEnumerable<HexCoord> AllHexes => _validHexes;

        // ── 占有管理 ───────────────────────────────────────────��──────
        public bool IsOccupied(HexCoord hex) => _occupants.ContainsKey(hex);

        public BattleUnit GetUnit(HexCoord hex)
        {
            _occupants.TryGetValue(hex, out var unit);
            return unit;
        }

        public bool PlaceUnit(BattleUnit unit, HexCoord hex)
        {
            if (!IsInBounds(hex) || IsOccupied(hex)) return false;
            _occupants[hex] = unit;
            unit.Position   = hex;
            return true;
        }

        public bool MoveUnit(BattleUnit unit, HexCoord to)
        {
            if (!IsInBounds(to) || IsOccupied(to)) return false;
            _occupants.Remove(unit.Position);
            _occupants[to] = unit;
            unit.Position  = to;
            return true;
        }

        public void RemoveUnit(BattleUnit unit)
        {
            _occupants.Remove(unit.Position);
        }

        // ── スポーン位置取得 ───────────────────────────────────────────
        /// <summary>プレイヤー側スポーン（Q &lt;= -1）を Q 昇順で返す。</summary>
        public List<HexCoord> GetLeftSpawnHexes() => GetSpawnHexes(q => q <= -1);

        /// <summary>敵側スポーン（Q &gt;= 1）を Q 昇順で返す。</summary>
        public List<HexCoord> GetRightSpawnHexes() => GetSpawnHexes(q => q >= 1);

        private List<HexCoord> GetSpawnHexes(Func<int, bool> qFilter)
        {
            var result = new List<HexCoord>();
            foreach (var hex in _validHexes)
                if (qFilter(hex.Q)) result.Add(hex);
            result.Sort((a, b) => a.Q != b.Q ? a.Q.CompareTo(b.Q) : a.R.CompareTo(b.R));
            return result;
        }
    }
}
