// Assets/Scripts/Framework/Battle/HexCoord.cs
// AgentSim — ヘックスグリッドのキューブ座標構造体
//
// Q, R を保持; S = -Q - R（暗黙）。
// Dictionary キーとして使うため IEquatable を実装する。
// Unity 非依存（System のみ）。

using System;
using System.Collections.Generic;

namespace AgentSim.Battle
{
    public readonly struct HexCoord : IEquatable<HexCoord>
    {
        public readonly int Q;
        public readonly int R;
        public int S => -Q - R;

        public HexCoord(int q, int r) { Q = q; R = r; }

        // ── 隣接 ──────────────────────────────���───────────────────────
        // flat-top hex の6方向（キューブ座標）
        private static readonly HexCoord[] _directions = new HexCoord[6]
        {
            new HexCoord( 1,  0), new HexCoord( 1, -1), new HexCoord( 0, -1),
            new HexCoord(-1,  0), new HexCoord(-1,  1), new HexCoord( 0,  1),
        };

        public static HexCoord[] AllDirections => _directions;

        /// <summary>隣接する6タイルを返す。</summary>
        public HexCoord[] Neighbors()
        {
            var result = new HexCoord[6];
            for (int i = 0; i < 6; i++)
                result[i] = new HexCoord(Q + _directions[i].Q, R + _directions[i].R);
            return result;
        }

        // ── 距離 ──────────────────────────────────────────────────────
        /// <summary>キューブ距離（ヘックス歩数）。</summary>
        public static int Distance(HexCoord a, HexCoord b)
        {
            return (Math.Abs(a.Q - b.Q) + Math.Abs(a.R - b.R) + Math.Abs(a.S - b.S)) / 2;
        }

        public int DistanceTo(HexCoord other) => Distance(this, other);

        // ── 範囲内列挙 ─────────────────────────────────────────────────
        /// <summary>中心を this として、半径 radius 以内の全タイルをリストで返す。</summary>
        public List<HexCoord> WithinRange(int radius)
        {
            var result = new List<HexCoord>();
            for (int q = -radius; q <= radius; q++)
            {
                int r1 = Math.Max(-radius, -q - radius);
                int r2 = Math.Min( radius, -q + radius);
                for (int r = r1; r <= r2; r++)
                    result.Add(new HexCoord(Q + q, R + r));
            }
            return result;
        }

        // ── 等値・ハッシュ ────────────────────────────────────────────
        public bool Equals(HexCoord other) => Q == other.Q && R == other.R;
        public override bool Equals(object obj) => obj is HexCoord h && Equals(h);
        public override int GetHashCode() => Q * 397 ^ R;
        public static bool operator ==(HexCoord a, HexCoord b) =>  a.Equals(b);
        public static bool operator !=(HexCoord a, HexCoord b) => !a.Equals(b);

        public override string ToString() => $"Hex({Q},{R})";
    }
}
