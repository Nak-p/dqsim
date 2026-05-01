using UnityEngine;
using UnityEngine.Tilemaps;

namespace DQSim.Battle
{
    /// <summary>BattleHexMap を Hexagonal Tilemap に描画する（ランタイム生成タイル）。</summary>
    public class BattleHexTilemapRenderer : MonoBehaviour
    {
        public Tilemap terrainTilemap;

        private Tile[] _terrainTiles;
        private bool _tilesBuilt;

        private void Awake() => EnsureTiles();

        public void RenderMap(BattleHexMap map)
        {
            EnsureTiles();
            if (terrainTilemap == null)
            {
                Debug.LogError("BattleHexTilemapRenderer: terrainTilemap is not assigned.");
                return;
            }

            terrainTilemap.ClearAllTiles();

            for (int x = 0; x < map.Width; x++)
            {
                for (int y = 0; y < map.Height; y++)
                {
                    var cell = new Vector3Int(x, y, 0);
                    var t = map.Get(x, y);
                    terrainTilemap.SetTile(cell, _terrainTiles[(int)t]);
                }
            }
        }

        private void EnsureTiles()
        {
            if (_tilesBuilt) return;
            BuildTiles();
            _tilesBuilt = true;
        }

        private void BuildTiles()
        {
            _terrainTiles = new Tile[5];
            _terrainTiles[(int)BattleTerrain.Plain] =
                MakeFlatTile(16, new Color32(0x8e, 0xc4, 0x6b, 0xff));
            _terrainTiles[(int)BattleTerrain.Forest] = MakeForestTile();
            _terrainTiles[(int)BattleTerrain.Mountain] = MakeMountainTile();
            _terrainTiles[(int)BattleTerrain.Swamp] =
                MakeFlatTile(16, new Color32(0x4a, 0x5d, 0x3a, 0xff));
            _terrainTiles[(int)BattleTerrain.Water] = MakeWaterTile();
        }

        private static Tile MakeFlatTile(int size, Color32 color)
        {
            var tex = new Texture2D(size, size) { filterMode = FilterMode.Point };
            var pixels = new Color32[size * size];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = color;
            tex.SetPixels32(pixels);
            tex.Apply();
            var tile = ScriptableObject.CreateInstance<Tile>();
            tile.sprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
            return tile;
        }

        private static Tile MakeWaterTile()
        {
            int size = 16;
            var tex = new Texture2D(size, size) { filterMode = FilterMode.Point };
            var pixels = new Color32[size * size];
            Color32 deep = new Color32(0x25, 0x63, 0xa8, 0xff);
            Color32 light = new Color32(0x4a, 0x90, 0xd9, 0xff);
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                    pixels[y * size + x] = ((x + y) % 4 == 0) ? light : deep;
            tex.SetPixels32(pixels);
            tex.Apply();
            var tile = ScriptableObject.CreateInstance<Tile>();
            tile.sprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
            return tile;
        }

        private static Tile MakeForestTile()
        {
            int size = 16;
            var tex = new Texture2D(size, size) { filterMode = FilterMode.Point };
            var pixels = new Color32[size * size];
            Color32 bg = new Color32(0x2d, 0x5e, 0x1e, 0xff);
            Color32 dot = new Color32(0x1a, 0x3d, 0x0f, 0xff);
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                    pixels[y * size + x] = (x % 4 == 2 && y % 4 == 2) ? dot : bg;
            tex.SetPixels32(pixels);
            tex.Apply();
            var tile = ScriptableObject.CreateInstance<Tile>();
            tile.sprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
            return tile;
        }

        private static Tile MakeMountainTile()
        {
            int size = 16;
            var tex = new Texture2D(size, size) { filterMode = FilterMode.Point };
            var pixels = new Color32[size * size];
            Color32 bg = new Color32(0x8c, 0x7a, 0x6b, 0xff);
            Color32 peak = new Color32(0xcc, 0xbb, 0xaa, 0xff);
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    int cx = x - 8;
                    pixels[y * size + x] = (Mathf.Abs(cx) <= (size - 1 - y) / 2 && y > 4) ? peak : bg;
                }
            tex.SetPixels32(pixels);
            tex.Apply();
            var tile = ScriptableObject.CreateInstance<Tile>();
            tile.sprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
            return tile;
        }
    }
}
