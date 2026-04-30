using System;
using System.Collections.Generic;
using UnityEngine;

namespace DQSim
{
    public class MissionManager : MonoBehaviour
    {
        public List<ActiveMission> ActiveMissions { get; } = new List<ActiveMission>();

        public event Action<ActiveMission> OnArrivalAtDestination;
        public event Action<ActiveMission> OnReturnedToBase;

        private TimeManager _timeManager;
        private MapData _map;
        private int _missionColorIndex;

        private static readonly Color32[] PartyColors =
        {
            new Color32(0xf0, 0xd0, 0x60, 0xff),
            new Color32(0x60, 0xc0, 0xf0, 0xff),
            new Color32(0xf0, 0x70, 0x70, 0xff),
            new Color32(0x70, 0xf0, 0x70, 0xff),
            new Color32(0xe0, 0x70, 0xe0, 0xff),
        };

        public void Initialize(TimeManager timeManager, MapData map)
        {
            _timeManager = timeManager;
            _map = map;
        }

        public ActiveMission StartMission(Quest quest, List<Adventurer> party)
        {
            var path = Pathfinder.FindPath(_map, _map.BasePosition, quest.Destination);
            if (path == null)
            {
                Debug.LogError($"No path to quest destination {quest.Destination}");
                return null;
            }

            // パーティ GameObject を動的生成
            var partyGO = new GameObject($"Party_{quest.Title}");
            var pc = partyGO.AddComponent<PartyController>();
            pc.Initialize(_timeManager, PartyColors[_missionColorIndex % PartyColors.Length]);
            _missionColorIndex++;

            float travelHours = TravelCalculator.CalculateTravelHours(path, _map, pc.partySpeed);
            var mission = new ActiveMission
            {
                Quest          = quest,
                Party          = party,
                PartyController = pc,
                ETAArrive      = _timeManager.CurrentTime + travelHours * 3600f,
            };

            pc.StartMoving(path, toDestination: true);
            pc.OnArrivedAtDestination += () => HandleArrivedAtDestination(mission);

            ActiveMissions.Add(mission);
            return mission;
        }

        private void HandleArrivedAtDestination(ActiveMission mission)
        {
            if (mission.State != ActiveMissionState.TravelingOut) return;

            OnArrivalAtDestination?.Invoke(mission);

            // 帰路パスを計算して自動帰還
            var returnPath = Pathfinder.FindPath(_map, mission.Quest.Destination, _map.BasePosition);
            if (returnPath == null)
            {
                Debug.LogError("No return path found");
                return;
            }

            float travelHours = TravelCalculator.CalculateTravelHours(returnPath, _map, mission.PartyController.partySpeed);
            mission.ETAReturn = _timeManager.CurrentTime + travelHours * 3600f;
            mission.State     = ActiveMissionState.TravelingBack;

            mission.PartyController.StartMoving(returnPath, toDestination: false);
            mission.PartyController.OnArrivedAtBase += () => HandleReturnedToBase(mission);
        }

        private void HandleReturnedToBase(ActiveMission mission)
        {
            if (mission.State != ActiveMissionState.TravelingBack) return;

            mission.State = ActiveMissionState.Complete;
            OnReturnedToBase?.Invoke(mission);

            ActiveMissions.Remove(mission);
            if (mission.PartyController != null)
                Destroy(mission.PartyController.gameObject);
        }
    }
}
