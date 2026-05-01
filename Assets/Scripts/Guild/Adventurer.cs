using System;

namespace DQSim
{
    public class Adventurer
    {
        public string Id { get; } = Guid.NewGuid().ToString();
        public string Name;
        public int Age;
        public AdventurerJob Job;
        public AdventurerRace Race;
        public AdventurerRank Rank;
        public AdventurerStats Stats;
        public bool IsAvailable = true;
        public int EarnedGold;

        public string StatusText => IsAvailable ? "Available" : "On Quest";

        // 名前プール
        private static readonly string[] HumanNames  = { "Aldric","Roland","Gareth","Owen","Marcus","Sera","Lyra","Mira","Elara","Tomas" };
        private static readonly string[] ElfNames     = { "Aelindra","Faelin","Sylvar","Thalion","Liriel","Caladwen","Erevan","Nimloth" };
        private static readonly string[] DwarfNames   = { "Borin","Durgin","Thora","Gimrak","Bryndis","Ulfgar","Dagny","Torvi" };

        public static Adventurer Generate(System.Random rng)
        {
            var race = (AdventurerRace)rng.Next(0, 3);
            var job  = (AdventurerJob)rng.Next(0, 3);
            var rank = rng.Next(0, 10) < 6 ? AdventurerRank.Copper
                     : rng.Next(0, 10) < 7 ? AdventurerRank.Silver
                     : AdventurerRank.Gold;

            string[] pool = race == AdventurerRace.Human ? HumanNames
                          : race == AdventurerRace.Elf   ? ElfNames
                          : DwarfNames;

            return new Adventurer
            {
                Name  = pool[rng.Next(pool.Length)],
                Age   = rng.Next(18, 55),
                Job   = job,
                Race  = race,
                Rank  = rank,
                Stats = AdventurerStats.Generate(job, race, rng),
            };
        }

        public string ShortLabel =>
            $"{Name}  [{AdventurerJobInfo.DisplayName(Job)}|{AdventurerRaceInfo.DisplayName(Race)}|{AdventurerRankInfo.DisplayName(Rank)}]";
    }
}
