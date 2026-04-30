using UnityEngine;
using UnityEngine.Tilemaps;

namespace DQSim
{
    public class TilemapRenderer : MonoBehaviour
    {
        [Header("Tilemaps")]
        public Tilemap terrainTilemap;
        public Tilemap markerTilemap;

        private Tile[] _terrainTiles;
        private Tile _baseTile;
        private Tile _destTile;

        private void Awake()
        {
            BuildTiles();
        }

        public void RenderMap(MapData map)
        {
            terrainTilemap.ClearAllTiles();
            markerTilemap.ClearAllTiles();

            for (int x = 0; x < MapData.Width; x++)
                for (int y = 0; y < MapData.Height; y++)
                    terrainTilemap.SetTile(new Vector3Int(x, y, 0), _terrainTiles[(int)map.Tiles[x, y]]);

            var baseCell = new Vector3Int(map.BasePosition.x, map.BasePosition.y, 0);
            var destCell = new Vector3Int(map.DestinationPosition.x, map.DestinationPosition.y, 0);
            markerTilemap.SetTile(baseCell, _baseTile);
            markerTilemap.SetTile(destCell, _destTile);
        }

        private void BuildTiles()
        {
            _terrainTiles = new Tile[5];
            _terrainTiles[(int)TileType.Grass]    = MakeFlatTile(16, new Color32(0x5a, 0x8a, 0x3c, 0xff));
            _terrainTiles[(int)TileType.Water]    = MakeWaterTile();
            _terrainTiles[(int)TileType.Forest]   = MakeForestTile();
            _terrainTiles[(int)TileType.Mountain] = MakeMountainTile();
            _terrainTiles[(int)TileType.Desert]   = MakeFlatTile(16, new Color32(0xd4, 0xaa, 0x70, 0xff));

            _baseTile = MakeBaseTile();
            _destTile = MakeDestTile();
        }

        // 単色タイル
        private Tile MakeFlatTile(int size, Color32 color)
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

        // 水: 青地に波模様
        private Tile MakeWaterTile()
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

        // 森: 緑に暗い木ドット
        private Tile MakeForestTile()
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

        // 山: 茶色地にグレーの三角
        private Tile MakeMountainTile()
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

        // 拠点: 白地に赤十字
        private Tile MakeBaseTile()
        {
            int size = 16;
            var tex = new Texture2D(size, size) { filterMode = FilterMode.Point };
            var pixels = new Color32[size * size];
            Color32 bg = new Color32(0xff, 0xff, 0xff, 0xff);
            Color32 cross = new Color32(0xcc, 0x22, 0x22, 0xff);
            Color32 border = new Color32(0x44, 0x44, 0x44, 0xff);
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    bool isBorder = x == 0 || x == size - 1 || y == 0 || y == size - 1;
                    bool isCross = (x >= 6 && x <= 9) || (y >= 6 && y <= 9);
                    pixels[y * size + x] = isBorder ? border : (isCross ? cross : bg);
                }
            tex.SetPixels32(pixels);
            tex.Apply();
            var tile = ScriptableObject.CreateInstance<Tile>();
            tile.sprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
            return tile;
        }

        // 目的地: 黄色に黒縁の星
        private Tile MakeDestTile()
        {
            int size = 16;
            var tex = new Texture2D(size, size) { filterMode = FilterMode.Point };
            var pixels = new Color32[size * size];
            Color32 bg = new Color32(0x22, 0x22, 0x88, 0xff);
            Color32 star = new Color32(0xff, 0xdd, 0x00, 0xff);
            // 簡易5角星パターン（手描きピクセル）
            int[] starRows = { 0b0000110000000000, 0b0001111000000000, 0b1111111111000000,
                               0b0111111110000000, 0b0011111100000000, 0b0101001010000000,
                               0b1001000001000000, 0b0000000000000000 };
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                    pixels[y * size + x] = bg;

            // 中央に簡単な星形を描く
            int[] sx = { 7, 7, 6, 8, 5, 9, 4, 10, 5, 9, 6, 8, 4, 5, 9, 10, 3, 11 };
            int[] sy = { 2, 3, 4, 4, 5, 5, 6, 6, 7, 7, 7, 7, 8, 8, 8, 8, 9, 9 };
            for (int i = 0; i < sx.Length; i++)
                if (sx[i] >= 0 && sx[i] < size && sy[i] >= 0 && sy[i] < size)
                    pixels[sy[i] * size + sx[i]] = star;

            // 縦の棒
            for (int y = 2; y <= 12; y++) pixels[y * size + 7] = star;
            for (int y = 2; y <= 12; y++) pixels[y * size + 8] = star;
            // 横の棒
            for (int x = 3; x <= 12; x++) pixels[7 * size + x] = star;
            for (int x = 3; x <= 12; x++) pixels[8 * size + x] = star;

            tex.SetPixels32(pixels);
            tex.Apply();
            var tile = ScriptableObject.CreateInstance<Tile>();
            tile.sprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
            return tile;
        }
    }
}
