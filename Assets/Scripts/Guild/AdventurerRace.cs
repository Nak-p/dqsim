namespace DQSim
{
    public enum AdventurerRace
    {
        Human,
        Elf,
        Dwarf
    }

    public static class AdventurerRaceInfo
    {
        public static string DisplayName(AdventurerRace race) => race switch
        {
            AdventurerRace.Human => "Human",
            AdventurerRace.Elf   => "Elf",
            AdventurerRace.Dwarf => "Dwarf",
            _ => "?"
        };

        // 種族ボーナス: Str, Vit, Mag, Wis, Fai
        public static int[] StatBonus(AdventurerRace race) => race switch
        {
            AdventurerRace.Human => new[] {  0,  0,  0,  0,  0 },
            AdventurerRace.Elf   => new[] { -2, -1,  3,  3, -1 },
            AdventurerRace.Dwarf => new[] {  3,  3, -2, -1,  2 },
            _ => new[] { 0, 0, 0, 0, 0 }
        };
    }
}
