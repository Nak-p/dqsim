using System;
using UnityEngine;

namespace DQSim
{
    public class Quest
    {
        public string Id { get; } = Guid.NewGuid().ToString();
        public string Title;
        public string Description;
        public string LocationName;
        public Vector2Int Destination;
        public int RewardGold;
        public AdventurerRank MinRank;
        public int MinPartySize;
        public int MaxPartySize;

        private static readonly string[] Titles = {
            "Goblin Extermination","Wolf Pack Hunt","Bandit Suppression",
            "Ancient Ruins Exploration","Orc Raid Repulsion","Cursed Forest Investigation",
            "Dragon Sighting Report","Missing Merchant Escort","Undead Purge","Troll Culling"
        };

        private static readonly string[] Locations = {
            "Eastern Forest","Northern Mountains","Southern Desert","Western Coast",
            "Ancient Ruins","Demon's Peak","Haunted Valley","Iron Mine","Sunken Temple","Frozen Pass"
        };

        public static Quest Generate(MapData map, System.Random rng, int questIndex)
        {
            var rank = (AdventurerRank)rng.Next(0, 3);
            int baseReward = rank switch {
                AdventurerRank.Copper => rng.Next(100, 350),
                AdventurerRank.Silver => rng.Next(350, 700),
                AdventurerRank.Gold   => rng.Next(700, 1500),
                _ => 200
            };

            string title    = Titles[questIndex % Titles.Length];
            string location = Locations[questIndex % Locations.Length];
            Vector2Int dest = map.GetRandomQuestTile(rng, map.BasePosition, minDist: 15);

            return new Quest
            {
                Title        = title,
                Description  = $"Quest at {location}. Defeat enemies and secure the area.",
                LocationName = location,
                Destination  = dest,
                RewardGold   = baseReward,
                MinRank      = rank,
                MinPartySize = 1,
                MaxPartySize = 4,
            };
        }
    }
}
