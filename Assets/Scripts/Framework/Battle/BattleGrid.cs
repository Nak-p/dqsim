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
        // ── 内部状態 ────────────────────────────────────────────────────
        private readonly HashSet<HexCoord>                _validHexes;
        private readonly Dictionary<HexCoord, BattleUnit> _occupants;

        public int Radius         { get; }
        /// <summary>スポーン境界となる Q 座標の絶対値。Q &lt;= -Threshold が左、Q &gt;= Threshold が右。</summary>
        public int SpawnThreshold { get; private set; }

        // ── Hex コンストラクタ ─────────────────────────────────────────
        public BattleGrid(int radius)
        {
            Radius         = radius;
            SpawnThreshold = 1;
            _validHexes    = new HashSet<HexCoord>();
            _occupants     = new Dictionary<HexCoord, BattleUnit>();

            var center = new HexCoord(0, 0);
            foreach (var hex in center.WithinRange(radius))
                _validHexes.Add(hex);
        }

        // ── プライベートファクトリコンストラクタ ─────────────────────────
        private BattleGrid(HashSet<HexCoord> hexes, int spawnThreshold)
        {
            Radius         = 0;
            SpawnThreshold = spawnThreshold;
            _validHexes    = hexes;
            _occupants     = new Dictionary<HexCoord, BattleUnit>();
        }

        // ── 矩形グリッドファクトリ ─────────────────────────────────────
        /// <summary>
        /// width × height の矩形ヘックスグリッドを生成する。
        /// Q in [-(width/2), -(width/2)+width-1]、R in [-(height/2), -(height/2)+height-1]。
        /// Unity Hexagonal Point Top Tilemap に対応。
        /// </summary>
        public static BattleGrid CreateRect(int width, int height)
        {
            int qMin      = -(width  / 2);
            int qMax      = qMin + width  - 1;
            int rMin      = -(height / 2);
            int rMax      = rMin + height - 1;
            int threshold = Math.Max(1, width / 4);

            var hexes = new HashSet<HexCoord>();
            for (int r = rMin; r <= rMax; r++)
                for (int q = qMin; q <= qMax; q++)
                    hexes.Add(new HexCoord(q, r));

            return new BattleGrid(hexes, threshold);
        }

        // ── 範囲チェック ────────────────────────────────────────────────
        public bool IsInBounds(HexCoord hex) => _validHexes.Contains(hex);

        public IEnumerable<HexCoord> AllHexes => _validHexes;

        // ── 占有管理 ────────────────────────────────────────────────────
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

        public void RemoveUnit(BattleUnit unit) => _occupants.Remove(unit.Position);

        // ── スポーン位置取得 ────────────────────────────────────────────
        /// <summary>プレイヤー側スポーン（Q &lt;= -SpawnThreshold）を Q 昇順で返す。</summary>
        public List<HexCoord> GetLeftSpawnHexes()  => GetSpawnHexes(q => q <= -SpawnThreshold);

        /// <summary>敵側スポーン（Q &gt;= SpawnThreshold）を Q 昇順で返す。</summary>
        public List<HexCoord> GetRightSpawnHexes() => GetSpawnHexes(q => q >=  SpawnThreshold);

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
