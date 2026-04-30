namespace DQSim
{
    public enum AdventurerJob
    {
        Warrior,
        Priest,
        Mage
    }

    public static class AdventurerJobInfo
    {
        public static string DisplayName(AdventurerJob job) => job switch
        {
            AdventurerJob.Warrior => "Warrior",
            AdventurerJob.Priest  => "Priest",
            AdventurerJob.Mage    => "Mage",
            _ => "?"
        };

        // 各職業の基本パラメータ範囲 (min, max) 順: Str, Vit, Mag, Wis, Fai
        public static (int min, int max)[] StatRanges(AdventurerJob job) => job switch
        {
            AdventurerJob.Warrior => new[] { (12,20),(10,18),( 2, 8),( 3, 9),( 3, 9) },
            AdventurerJob.Priest  => new[] { ( 4,10),( 6,14),( 5,13),(10,18),(12,20) },
            AdventurerJob.Mage    => new[] { ( 2, 8),( 3, 9),(14,22),(12,20),( 3, 9) },
            _ => new[] { (5,15),(5,15),(5,15),(5,15),(5,15) }
        };
    }
}
