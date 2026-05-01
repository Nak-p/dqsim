using UnityEngine;

namespace DQSim.Battle
{
    /// <summary>シード付き Perlin で地形を割り当てるバトルフィールド生成。</summary>
    public static class BattleFieldGenerator
    {
        public const int DefaultWidth = 24;
        public const int DefaultHeight = 18;

        public static BattleHexMap Generate(int seed, int width = DefaultWidth, int height = DefaultHeight)
        {
            var map = new BattleHexMap(width, height);
            float ox = seed * 0.11f;
            float oy = seed * 0.07f;

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    float nx = x * 0.09f + ox;
                    float ny = y * 0.09f + oy;
                    float elevation = Mathf.PerlinNoise(nx, ny);
                    float moisture = Mathf.PerlinNoise(nx + 100f, ny + 100f);
                    map.Set(x, y, Classify(elevation, moisture));
                }
            }

            return map;
        }

        private static BattleTerrain Classify(float elevation, float moisture)
        {
            if (elevation < 0.35f) return BattleTerrain.Water;
            if (elevation > 0.72f) return BattleTerrain.Mountain;
            if (elevation < 0.48f && moisture > 0.55f) return BattleTerrain.Swamp;
            if (moisture > 0.52f) return BattleTerrain.Forest;
            return BattleTerrain.Plain;
        }
    }
}
