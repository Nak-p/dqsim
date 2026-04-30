namespace DQSim
{
    public struct AdventurerStats
    {
        public int Strength;
        public int Vitality;
        public int Magic;
        public int Wisdom;
        public int Faith;

        public static AdventurerStats Generate(AdventurerJob job, AdventurerRace race, System.Random rng)
        {
            var ranges = AdventurerJobInfo.StatRanges(job);
            var bonus  = AdventurerRaceInfo.StatBonus(race);
            return new AdventurerStats
            {
                Strength = Clamp(rng.Next(ranges[0].min, ranges[0].max + 1) + bonus[0]),
                Vitality = Clamp(rng.Next(ranges[1].min, ranges[1].max + 1) + bonus[1]),
                Magic    = Clamp(rng.Next(ranges[2].min, ranges[2].max + 1) + bonus[2]),
                Wisdom   = Clamp(rng.Next(ranges[3].min, ranges[3].max + 1) + bonus[3]),
                Faith    = Clamp(rng.Next(ranges[4].min, ranges[4].max + 1) + bonus[4]),
            };
        }

        private static int Clamp(int v) => v < 1 ? 1 : v;

        public override string ToString() =>
            $"STR:{Strength} VIT:{Vitality} MAG:{Magic} WIS:{Wisdom} FAI:{Faith}";
    }
}
