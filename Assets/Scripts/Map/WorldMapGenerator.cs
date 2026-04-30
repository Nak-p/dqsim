using UnityEngine;

namespace DQSim
{
    public class WorldMapGenerator : MonoBehaviour
    {
        public MapData Map { get; private set; }

        public void Generate(int seed = 0)
        {
            for (int attempt = 0; attempt < 10; attempt++)
            {
                var map = GenerateMap(seed + attempt * 1000);
                var path = Pathfinder.FindPath(map, map.BasePosition, map.DestinationPosition);
                if (path != null && path.Count > 1)
                {
                    Map = map;
                    Debug.Log($"Map generated (attempt {attempt + 1}): base={map.BasePosition}, dest={map.DestinationPosition}, path={path.Count} tiles");
                    return;
                }
            }
            Debug.LogError("Map generation failed: no path found");
        }

        private MapData GenerateMap(int seed)
        {
            var map = new MapData();
            float offsetX = seed * 0.1f;
            float offsetY = seed * 0.07f;

            for (int x = 0; x < MapData.Width; x++)
            {
                for (int y = 0; y < MapData.Height; y++)
                {
                    float e = Mathf.PerlinNoise(x * 0.05f + offsetX, y * 0.05f + offsetY);
                    float m = Mathf.PerlinNoise(x * 0.08f + offsetX + 100f, y * 0.08f + offsetY + 100f);

                    map.Tiles[x, y] = ClassifyTile(e, m);
                }
            }

            map.BasePosition = FindGrassTile(map, 4, 4, MapData.Width / 2 - 1, MapData.Height / 2 - 1);
            map.DestinationPosition = FindGrassTile(map, MapData.Width / 2 + 4, MapData.Height / 2 + 4,
                MapData.Width - 5, MapData.Height - 5);

            // 目的地が拠点から十分離れていなければ強制配置
            if (Vector2Int.Distance(map.BasePosition, map.DestinationPosition) < 20f)
                map.DestinationPosition = ForceGrassTile(map, MapData.Width - 10, MapData.Height - 10);

            return map;
        }

        private TileType ClassifyTile(float e, float m)
        {
            if (e < 0.35f) return TileType.Water;
            if (e < 0.50f && m > 0.55f) return TileType.Forest;
            if (e > 0.75f) return TileType.Mountain;
            if (e < 0.55f && m < 0.30f) return TileType.Desert;
            return TileType.Grass;
        }

        private Vector2Int FindGrassTile(MapData map, int minX, int minY, int maxX, int maxY)
        {
            for (int y = minY; y <= maxY; y++)
                for (int x = minX; x <= maxX; x++)
                    if (map.Tiles[x, y] == TileType.Grass)
                        return new Vector2Int(x, y);

            // Grassが見つからなければ最初の歩行可能タイルを返す
            for (int y = minY; y <= maxY; y++)
                for (int x = minX; x <= maxX; x++)
                    if (map.IsWalkable(x, y))
                        return new Vector2Int(x, y);

            return new Vector2Int(minX, minY);
        }

        private Vector2Int ForceGrassTile(MapData map, int startX, int startY)
        {
            // 近くにGrassタイルを探す（周囲15タイル）
            for (int r = 0; r < 15; r++)
                for (int dy = -r; dy <= r; dy++)
                    for (int dx = -r; dx <= r; dx++)
                    {
                        int x = Mathf.Clamp(startX + dx, 0, MapData.Width - 1);
                        int y = Mathf.Clamp(startY + dy, 0, MapData.Height - 1);
                        if (map.IsWalkable(x, y))
                            return new Vector2Int(x, y);
                    }
            return new Vector2Int(startX, startY);
        }
    }
}
