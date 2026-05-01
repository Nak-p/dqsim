namespace DQSim.Battle
{
    /// <summary>移動コストと通行可否（後続の経路探索・ユニット移動で使用）。</summary>
    public static class BattleTerrainCost
    {
        private const float Blocked = 999f;

        private static readonly float[] Costs =
        {
            1.0f,  // Plain
            1.5f,  // Forest
            Blocked, // Mountain
            2.0f,  // Swamp
            Blocked  // Water
        };

        public static float Get(BattleTerrain terrain) => Costs[(int)terrain];

        public static bool IsWalkable(BattleTerrain terrain) => Get(terrain) < Blocked;
    }
}
