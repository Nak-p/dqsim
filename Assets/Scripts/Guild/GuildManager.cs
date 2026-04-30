using System;
using System.Collections.Generic;
using UnityEngine;

namespace DQSim
{
    public class GuildManager : MonoBehaviour
    {
        public List<Adventurer> Adventurers { get; } = new List<Adventurer>();
        public List<Quest> AvailableQuests  { get; } = new List<Quest>();
        public int GuildGold { get; private set; } = 1000;

        public event Action OnGuildStateChanged;

        private MissionManager _missionManager;
        private MapData _map;
        private System.Random _rng;
        private TimeManager _timeManager;

        private float _lastQuestGenDay = -1f;
        private const float QuestGenIntervalDays = 3f;
        private int _questCounter = 0;

        public void Initialize(MapData map, MissionManager missionManager, TimeManager timeManager, int seed = 42)
        {
            _map = map;
            _missionManager = missionManager;
            _timeManager = timeManager;
            _rng = new System.Random(seed);

            GenerateAdventurers(10);
            GenerateInitialQuests(4);

            _missionManager.OnReturnedToBase += HandleMissionComplete;
        }

        private void Update()
        {
            if (_timeManager == null || _timeManager.IsPaused) return;

            float currentDay = _timeManager.CurrentTime.Day;
            if (_lastQuestGenDay < 0) _lastQuestGenDay = currentDay;

            if (currentDay - _lastQuestGenDay >= QuestGenIntervalDays)
            {
                _lastQuestGenDay = currentDay;
                if (AvailableQuests.Count < 6)
                {
                    AvailableQuests.Add(Quest.Generate(_map, _rng, _questCounter++));
                    OnGuildStateChanged?.Invoke();
                }
            }
        }

        public bool CanDispatch(Quest quest, List<Adventurer> party)
        {
            if (party == null || party.Count < quest.MinPartySize) return false;
            foreach (var a in party)
                if (!a.IsAvailable) return false;
            return true;
        }

        public void DispatchParty(Quest quest, List<Adventurer> party)
        {
            if (!CanDispatch(quest, party)) return;

            AvailableQuests.Remove(quest);
            foreach (var a in party) a.IsAvailable = false;

            _missionManager.StartMission(quest, party);
            OnGuildStateChanged?.Invoke();
        }

        private void HandleMissionComplete(ActiveMission mission)
        {
            GuildGold += mission.Quest.RewardGold;
            foreach (var a in mission.Party) a.IsAvailable = true;
            OnGuildStateChanged?.Invoke();
        }

        private void GenerateAdventurers(int count)
        {
            for (int i = 0; i < count; i++)
                Adventurers.Add(Adventurer.Generate(_rng));
        }

        private void GenerateInitialQuests(int count)
        {
            for (int i = 0; i < count; i++)
                AvailableQuests.Add(Quest.Generate(_map, _rng, _questCounter++));
        }
    }
}
