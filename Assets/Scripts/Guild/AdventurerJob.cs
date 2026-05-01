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

        /// <summary>ステ配分の職業ウエイト (Str, Vit, Mag, Wis, Fai)。強み≈3 / 弱み≈0.4 の中等特化。</summary>
        public static float[] StatWeights(AdventurerJob job) => job switch
        {
            AdventurerJob.Warrior => new[] { 3.0f, 2.5f, 0.4f, 0.6f, 0.6f },
            AdventurerJob.Priest  => new[] { 0.5f, 1.2f, 1.0f, 2.5f, 3.0f },
            AdventurerJob.Mage    => new[] { 0.4f, 0.5f, 3.0f, 2.5f, 0.6f },
            _ => new[] { 1f, 1f, 1f, 1f, 1f }
        };

        /// <summary>ナッジ時の優先順位（前方ほど重要）。フィールド番号: 0=Str,1=Vit,2=Mag,3=Wis,4=Fai</summary>
        public static int[] StatPriority(AdventurerJob job) => job switch
        {
            AdventurerJob.Warrior => new[] { 0, 1, 4, 3, 2 },
            AdventurerJob.Priest  => new[] { 4, 3, 1, 2, 0 },
            AdventurerJob.Mage    => new[] { 2, 3, 4, 1, 0 },
            _ => new[] { 0, 1, 2, 3, 4 }
        };
    }
}
