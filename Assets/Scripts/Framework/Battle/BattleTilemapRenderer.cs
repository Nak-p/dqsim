// Assets/Scripts/Framework/Battle/BattleTilemapRenderer.cs
// AgentSim — ヘックスバトルグリッドを Unity Tilemap に描画する静的ユーティリティ
//
// 色は BattleVisualConfig (battle_visual.json) から読み込む。
// C# に色定数・パターン数値をハードコーディングしてはいけない。
//
// Unity Grid 設定: Cell Layout = Hexagonal Point Top
// 座標変換: HexCoord (axial) → Vector3Int (tilemap offset)

using UnityEngine;
using UnityEngine.Tilemaps;
using AgentSim.Config;

namespace AgentSim.Battle
{
    public static class BattleTilemapRenderer
    {
        // テクスチャサイズ（ピクセル）— アルゴリズム定数
        private const int TileSize = 64;

        // ── 公開 API ──────────────────────────────────────────────────
        public static void Render(BattleGrid grid, Tilemap tilemap)
        {
            tilemap.ClearAllTiles();
            var visual = SettingsRegistry.Current?.BattleVisual;

            foreach (var hex in grid.AllHexes)
            {
                string tileId = GetTileId(hex, grid);
                var color     = GetTileColor(tileId, visual);
                var tile      = BuildTile(color);
                tilemap.SetTile(HexToTilemapPos(hex), tile);
            }
        }

        /// <summary>
        /// HexCoord (axial) → Unity Tilemap 座標。
        /// Hexagonal Point Top グリッドに対応。
        /// </summary>
        public static Vector3Int HexToTilemapPos(HexCoord hex)
            => new Vector3Int(hex.Q, hex.R, 0);

        // ── 内部ロジック ──────────────────────────────────────────────
        private static string GetTileId(HexCoord hex, BattleGrid grid)
        {
            int threshold = grid.SpawnThreshold;
            if (hex.Q <= -threshold) return "player_spawn";
            if (hex.Q >=  threshold) return "enemy_spawn";
            return "floor";
        }

        private static Color GetTileColor(string tileId, BattleVisualConfig visual)
        {
            if (visual?.battle_tiles != null)
                foreach (var def in visual.battle_tiles)
                    if (def.id == tileId && def.color != null && def.color.Length >= 4)
                        return new Color(def.color[0], def.color[1], def.color[2], def.color[3]);

            return tileId switch
            {
                "player_spawn" => new Color(0.2f, 0.3f, 0.5f),
                "enemy_spawn"  => new Color(0.5f, 0.2f, 0.2f),
                _              => new Color(0.25f, 0.25f, 0.3f),
            };
        }

        private static Tile BuildTile(Color color)
        {
            var pixels = DrawHexTile(TileSize, color);
            var tex    = new Texture2D(TileSize, TileSize, TextureFormat.RGBA32, false)
                         { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            tex.SetPixels(pixels);
            tex.Apply();

            var tile    = ScriptableObject.CreateInstance<Tile>();
            tile.sprite = Sprite.Create(tex,
                new Rect(0, 0, TileSize, TileSize),
                new Vector2(0.5f, 0.5f), TileSize);
            return tile;
        }

        private static Color[] DrawHexTile(int size, Color fill)
        {
            var pixels  = new Color[size * size];
            var border  = new Color(fill.r * 0.45f, fill.g * 0.45f, fill.b * 0.45f, 1f);

            float cx      = size * 0.5f;
            float cy      = size * 0.5f;
            float outerR  = size * 0.47f;
            float softW   = size * 0.025f;
            float borderW = size * 0.05f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float px = x + 0.5f - cx;
                    float py = y + 0.5f - cy;

                    // Point Top: px と py を入れ替えて 90° 回転
                    float dist = HexDistance(py / outerR, px / outerR);

                    if (dist >= 1f + softW / outerR)
                        pixels[y * size + x] = Color.clear;
                    else if (dist >= 1f - softW / outerR)
                    {
                        float t = (dist - (1f - softW / outerR)) / (2f * softW / outerR);
                        pixels[y * size + x] = new Color(fill.r, fill.g, fill.b, 1f - Mathf.Clamp01(t));
                    }
                    else if (dist >= 1f - (softW + borderW) / outerR)
                        pixels[y * size + x] = border;
                    else
                        pixels[y * size + x] = fill;
                }
            }
            return pixels;
        }

        /// <summary>Flat-top hex 距離（入れ替えて使うことで Point-top になる）。</summary>
        private static float HexDistance(float px, float py)
        {
            const float Sqrt3Over2 = 0.8660254f;
            float a = Mathf.Abs(px);
            float b = Mathf.Abs(px * 0.5f + py * Sqrt3Over2);
            float c = Mathf.Abs(px * 0.5f - py * Sqrt3Over2);
            return Mathf.Max(a, Mathf.Max(b, c));
        }
    }
}
