namespace DQSim
{
    public enum TileType
    {
        Grass,
        Water,
        Forest,
        Mountain,
        Desert
    }

    public static class TileMovementCost
    {
        private static readonly float[] Costs = { 1.0f, 999f, 1.5f, 3.0f, 2.0f };

        public static float Get(TileType type) => Costs[(int)type];

        public static bool IsWalkable(TileType type) => Get(type) < 999f;
    }
}
