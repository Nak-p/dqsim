using System.Collections.Generic;
using UnityEngine;

namespace DQSim.Battle
{
    /// <summary>
    /// Unity の Hexagonal Point Top（奇数行が右に半セルずれる）に合わせた odd-r オフセット座標の隣接。
    /// 参照: https://www.redblobgames.com/grids/hexagons/#neighbors-offset
    /// Unity マニュアル: Hexagonal Point Top は奇数行がオフセット。
    /// </summary>
    public static class HexCoordinates
    {
        // odd-r: index 0 = even rows, index 1 = odd rows
        private static readonly Vector2Int[][] OddRNeighborDeltas =
        {
            new[]
            {
                new Vector2Int(1, 0),
                new Vector2Int(0, 1),
                new Vector2Int(-1, 1),
                new Vector2Int(-1, 0),
                new Vector2Int(-1, -1),
                new Vector2Int(0, -1),
            },
            new[]
            {
                new Vector2Int(1, 0),
                new Vector2Int(1, 1),
                new Vector2Int(0, 1),
                new Vector2Int(-1, 0),
                new Vector2Int(0, -1),
                new Vector2Int(1, -1),
            }
        };

        /// <summary>odd-r オフセット (col=x, row=y) から軸座標 (q, r) へ。</summary>
        public static Vector2Int OffsetToAxial(int col, int row)
        {
            int q = col - (row - (row & 1)) / 2;
            int r = row;
            return new Vector2Int(q, r);
        }

        /// <summary>軸座標から odd-r オフセットへ。</summary>
        public static Vector2Int AxialToOffset(int q, int r)
        {
            int col = q + (r - (r & 1)) / 2;
            int row = r;
            return new Vector2Int(col, row);
        }

        /// <summary>6 近傍（マップ範囲内のみ）。</summary>
        public static void GetNeighbors(BattleHexMap map, int x, int y, List<Vector2Int> buffer)
        {
            buffer.Clear();
            int parity = y & 1;
            var deltas = OddRNeighborDeltas[parity];
            for (int i = 0; i < deltas.Length; i++)
            {
                int nx = x + deltas[i].x;
                int ny = y + deltas[i].y;
                if (map.InBounds(nx, ny))
                    buffer.Add(new Vector2Int(nx, ny));
            }
        }
    }
}
