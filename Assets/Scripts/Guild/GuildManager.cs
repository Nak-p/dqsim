using System;
using System.Collections.Generic;
using UnityEngine;

namespace DQSim
{
    public class GuildManager : MonoBehaviour
    {
        private const int GuildSharePercent = 20;

        public List<Adventurer> Adventurers { get; } = new List<Adventurer>();
        public List<Quest> AvailableQuests  { get; } = new List<Quest>();
        public int GuildGold { get; private set; } = 1000;
        public int TotalBountyEarned { get; private set; }

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
            int reward = mission.Quest.RewardGold;
            int guildShare = reward * GuildSharePercent / 100;
            GuildGold += guildShare;

            int partyCount = mission.Party?.Count ?? 0;
            if (partyCount > 0)
            {
                int adventurerPool = reward - guildShare;
                float totalWeight = 0f;
                foreach (var a in mission.Party)
                    totalWeight += AdventurerRankInfo.RewardWeight(a.Rank);
                if (totalWeight <= 0f)
                    totalWeight = partyCount;

                var payouts = new int[partyCount];
                var fractional = new float[partyCount];
                int distributed = 0;

                for (int i = 0; i < partyCount; i++)
                {
                    var adv = mission.Party[i];
                    float w = AdventurerRankInfo.RewardWeight(adv.Rank);
                    float exact = adventurerPool * (w / totalWeight);
                    int payout = Mathf.FloorToInt(exact);
                    payouts[i] = payout;
                    fractional[i] = exact - payout;
                    distributed += payout;
                }

                int remainder = adventurerPool - distributed;
                // 端数は「小数部が大きい + 高ランク」の順で配分し、相関を明確にする。
                while (remainder > 0)
                {
                    int bestIndex = 0;
                    float bestFrac = float.MinValue;
                    int bestRank = int.MinValue;
                    for (int i = 0; i < partyCount; i++)
                    {
                        int rankValue = (int)mission.Party[i].Rank;
                        if (fractional[i] > bestFrac ||
                            (Mathf.Approximately(fractional[i], bestFrac) && rankValue > bestRank))
                        {
                            bestFrac = fractional[i];
                            bestRank = rankValue;
                            bestIndex = i;
                        }
                    }

                    payouts[bestIndex]++;
                    fractional[bestIndex] = -1f;
                    remainder--;
                }

                for (int i = 0; i < partyCount; i++)
                {
                    var adv = mission.Party[i];
                    int payout = payouts[i];
                    adv.EarnedGold += payout;
                    adv.CurrentGold += payout;
                    TotalBountyEarned += payout;
                }
            }

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
