// Assets/Scripts/Framework/Pathfinding/TravelCalculator.cs
// AgentSim — パスの移動コストをゲーム内時間（時間単位）に変換する
//
// 移動速度は game_config.json の agent_travel_speed を参照する。

using System.Collections.Generic;
using UnityEngine;
using AgentSim.Config;
using AgentSim.Map;

namespace AgentSim.Pathfinding
{
    public static class TravelCalculator
    {
        /// <summary>
        /// パスの総移動コストをゲーム内時間（時間単位）に変換して返す。
        /// 移動速度は SettingsRegistry.Current.Game.agent_travel_speed を自動参照する。
        /// </summary>
        public static float CalculateTravelHours(List<Vector2Int> path, MapData map)
        {
            float speed = SettingsRegistry.Current?.Game?.agent_travel_speed ?? 1f;
            return CalculateTravelHours(path, map, speed);
        }

        /// <summary>
        /// 移動速度を明示指定するオーバーロード。
        /// partySpeed の単位は タイル/ゲーム内時間。
        /// </summary>
        public static float CalculateTravelHours(List<Vector2Int> path, MapData map, float partySpeed)
        {
            if (path == null || path.Count <= 1 || partySpeed <= 0f) return 0f;

            float totalCost = 0f;
            for (int i = 1; i < path.Count; i++)
                totalCost += map.MovementCost(path[i].x, path[i].y);

            return totalCost / partySpeed;
        }
    }
}
