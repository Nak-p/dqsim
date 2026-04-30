using System.Collections.Generic;
using UnityEngine;

namespace DQSim
{
    public static class TravelCalculator
    {
        /// <summary>
        /// パスの総移動コストを「ゲーム内時間（時間単位）」に変換して返す。
        /// partySpeed はタイル/ゲーム内時間。
        /// </summary>
        public static float CalculateTravelHours(List<Vector2Int> path, MapData map, float partySpeed)
        {
            if (path == null || path.Count <= 1 || partySpeed <= 0f)
                return 0f;

            float totalCost = 0f;
            for (int i = 1; i < path.Count; i++)
                totalCost += map.MovementCost(path[i].x, path[i].y);

            return totalCost / partySpeed;
        }
    }
}
