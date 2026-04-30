using UnityEngine;

namespace DQSim
{
    public enum AdventurerRank
    {
        Copper = 0,
        Silver = 1,
        Gold   = 2
    }

    public static class AdventurerRankInfo
    {
        public static string DisplayName(AdventurerRank rank) => rank switch
        {
            AdventurerRank.Copper => "Copper",
            AdventurerRank.Silver => "Silver",
            AdventurerRank.Gold   => "Gold",
            _ => "?"
        };

        public static Color BadgeColor(AdventurerRank rank) => rank switch
        {
            AdventurerRank.Copper => new Color(0.72f, 0.45f, 0.20f),
            AdventurerRank.Silver => new Color(0.75f, 0.75f, 0.80f),
            AdventurerRank.Gold   => new Color(0.95f, 0.80f, 0.10f),
            _ => Color.white
        };
    }
}
