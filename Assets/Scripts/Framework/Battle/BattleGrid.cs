// Assets/Scripts/Framework/Battle/BattleGrid.cs
// AgentSim — ヘックスバトルグリッドの状態管理
//
// 座標系: Q=縦軸(Y in Tilemap)、R=横軸(X in Tilemap)
// スポーン: R <= -SpawnThreshold = 左(Player)、R >= SpawnThreshold = 右(Enemy)
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
        /// <summary>
        /// スポーン境界。R &lt;= -SpawnThreshold が左(Player)、R &gt;= SpawnThreshold が右(Enemy)。
        /// </summary>
        public int SpawnThreshold { get; private set; }

        // ── Hex コンストラクタ（六角形フィールド）───────────────────────
        public BattleGrid(int radius)
        {
            Radius         = radius;
            SpawnThreshold = Math.Max(1, radius - 1);
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
        /// R が横軸 (width 個)、Q が縦軸 (height 個)。
        /// Unity Hexagonal Tilemap: HexToTilemapPos = (Q, R, 0) → X=Q(縦), Y=R(横)。
        /// </summary>
        public static BattleGrid CreateRect(int width, int height)
        {
            // R = 横軸 → width で決まる
            int rMin = -(width  / 2);
            int rMax = rMin + width  - 1;
            // Q = 縦軸 → height で決まる
            int qMin = -(height / 2);
            int qMax = qMin + height - 1;
            // スポーン幅 = 横(R)の約 1/3
            int threshold = Math.Max(1, width / 3);

            var hexes = new HashSet<HexCoord>();
            for (int q = qMin; q <= qMax; q++)
                for (int r = rMin; r <= rMax; r++)
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
        /// <summary>
        /// 左(Player)スポーン: R &lt;= -SpawnThreshold。
        /// 内側（中央寄り）→外側、縦中央行から順に返す。
        /// </summary>
        public List<HexCoord> GetLeftSpawnHexes()
        {
            var result = new List<HexCoord>();
            foreach (var hex in _validHexes)
                if (hex.R <= -SpawnThreshold) result.Add(hex);

            // R 降順（内側=0に近い方が先）、同R内は |Q| 昇順（縦中央が先）
            result.Sort((a, b) =>
            {
                if (a.R != b.R) return b.R.CompareTo(a.R);
                int qa = Math.Abs(a.Q), qb = Math.Abs(b.Q);
                return qa != qb ? qa.CompareTo(qb) : b.Q.CompareTo(a.Q);
            });
            return result;
        }

        /// <summary>
        /// 右(Enemy)スポーン: R &gt;= SpawnThreshold。
        /// 内側（中央寄り）→外側、縦中央行から順に返す。
        /// </summary>
        public List<HexCoord> GetRightSpawnHexes()
        {
            var result = new List<HexCoord>();
            foreach (var hex in _validHexes)
                if (hex.R >= SpawnThreshold) result.Add(hex);

            // R 昇順（内側=0に近い方が先）、同R内は |Q| 昇順（縦中央が先）
            result.Sort((a, b) =>
            {
                if (a.R != b.R) return a.R.CompareTo(b.R);
                int qa = Math.Abs(a.Q), qb = Math.Abs(b.Q);
                return qa != qb ? qa.CompareTo(qb) : b.Q.CompareTo(a.Q);
            });
            return result;
        }
    }
}
