using UnityEngine;

namespace DQSim
{
    public enum AdventurerRank
    {
        Copper  = 0,
        Iron    = 1,
        Silver  = 2,
        Gold    = 3,
        Mithril = 4
    }

    public static class AdventurerRankInfo
    {
        public static string DisplayName(AdventurerRank rank) => rank switch
        {
            AdventurerRank.Copper  => "Copper",
            AdventurerRank.Iron    => "Iron",
            AdventurerRank.Silver  => "Silver",
            AdventurerRank.Gold    => "Gold",
            AdventurerRank.Mithril => "Mithril",
            _ => "?"
        };

        public static Color BadgeColor(AdventurerRank rank) => rank switch
        {
            AdventurerRank.Copper  => new Color(0.72f, 0.45f, 0.20f),
            AdventurerRank.Iron    => new Color(0.36f, 0.35f, 0.34f),
            AdventurerRank.Silver  => new Color(0.75f, 0.75f, 0.80f),
            AdventurerRank.Gold    => new Color(0.95f, 0.80f, 0.10f),
            AdventurerRank.Mithril => new Color(0.78f, 0.91f, 1.00f),
            _ => Color.white
        };

        /// <summary>各ランクの総合力のガウス分布パラメータ（隣接ランクと重なり外れ値が出る）</summary>
        public static (float mean, float stddev) PowerProfile(AdventurerRank r) => r switch
        {
            AdventurerRank.Copper  => (30f, 8f),
            AdventurerRank.Iron    => (55f, 9f),
            AdventurerRank.Silver  => (80f, 10f),
            AdventurerRank.Gold    => (110f, 11f),
            AdventurerRank.Mithril => (150f, 13f),
            _ => (50f, 10f)
        };

        /// <summary>ギルド母集団での出現比率（高ランクほど稀）</summary>
        public static float Population(AdventurerRank r) => r switch
        {
            AdventurerRank.Copper  => 0.40f,
            AdventurerRank.Iron    => 0.28f,
            AdventurerRank.Silver  => 0.18f,
            AdventurerRank.Gold    => 0.10f,
            AdventurerRank.Mithril => 0.04f,
            _ => 0f
        };

        /// <summary>報酬配分のランク重み（高ランクほど取り分が増える）。</summary>
        public static float RewardWeight(AdventurerRank r) => r switch
        {
            AdventurerRank.Copper  => 1.00f,
            AdventurerRank.Iron    => 1.60f,
            AdventurerRank.Silver  => 2.40f,
            AdventurerRank.Gold    => 3.60f,
            AdventurerRank.Mithril => 5.40f,
            _ => 1.0f
        };

        /// <summary>Population に基づきランクを抽選する。</summary>
        public static AdventurerRank PickRank(System.Random rng)
        {
            double x = rng.NextDouble();
            if (x < 0.40) return AdventurerRank.Copper;
            if (x < 0.68) return AdventurerRank.Iron;    // 0.40 + 0.28
            if (x < 0.86) return AdventurerRank.Silver;  // + 0.18
            if (x < 0.96) return AdventurerRank.Gold;   // + 0.10
            return AdventurerRank.Mithril;               // + 0.04
        }
    }
}
