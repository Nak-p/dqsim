// Assets/Scripts/Framework/Battle/BattleTilemapRenderer.cs
// AgentSim — ヘックスバトルグリッドを Unity Tilemap に描画する静的ユーティリティ
//
// 色は BattleVisualConfig (battle_visual.json) から読み込む。
// C# に色定数・パターン数値をハードコーディングしてはいけない。
//
// Unity Grid 設定: Cell Layout = Hexagonal Flat Top
// 座標変換: HexCoord (axial) → Vector3Int (tilemap offset)

using UnityEngine;
using UnityEngine.Tilemaps;
using AgentSim.Config;

namespace AgentSim.Battle
{
    public static class BattleTilemapRenderer
    {
        // テクスチャサイズ（ピクセル）— アルゴリズム定数
        // 64px で滑らかなヘックス形状を実現する
        private const int TileSize = 64;

        // ── 公開 API ──────────────────────────────────────────────────
        /// <summary>
        /// BattleGrid の全タイルを Tilemap に描画する。
        /// SettingsRegistry.Current.BattleVisual が null の場合はグレーのフォールバック色を使う。
        /// </summary>
        public static void Render(BattleGrid grid, Tilemap tilemap)
        {
            tilemap.ClearAllTiles();

            var visual = SettingsRegistry.Current?.BattleVisual;

            foreach (var hex in grid.AllHexes)
            {
                string tileId = GetTileId(hex);
                var color     = GetTileColor(tileId, visual);
                var tile      = BuildTile(color);
                tilemap.SetTile(HexToTilemapPos(hex), tile);
            }
        }

        /// <summary>
        /// HexCoord (axial) → Unity Tilemap オフセット座標。
        /// Grid の Cell Layout が Hexagonal Flat Top の場合に有効。
        /// </summary>
        public static Vector3Int HexToTilemapPos(HexCoord hex)
            => new Vector3Int(hex.Q, hex.R, 0);

        // ── 内部ロジック ──────────────────────────────────────────────
        private static string GetTileId(HexCoord hex)
        {
            if (hex.Q <= -1) return "player_spawn";
            if (hex.Q >=  1) return "enemy_spawn";
            return "floor";
        }

        private static Color GetTileColor(string tileId, BattleVisualConfig visual)
        {
            if (visual?.battle_tiles != null)
            {
                foreach (var def in visual.battle_tiles)
                {
                    if (def.id == tileId && def.color != null && def.color.Length >= 4)
                        return new Color(def.color[0], def.color[1], def.color[2], def.color[3]);
                }
            }
            // フォールバック
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
            // Bilinear フィルタで補間し、ヘックス境界をなめらかに表示
            var tex    = new Texture2D(TileSize, TileSize, TextureFormat.RGBA32, false)
                         { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            tex.SetPixels(pixels);
            tex.Apply();

            var tile    = ScriptableObject.CreateInstance<Tile>();
            tile.sprite = Sprite.Create(tex,
                new Rect(0, 0, TileSize, TileSize),
                new Vector2(0.5f, 0.5f),
                TileSize);
            return tile;
        }

        /// <summary>
        /// ヘックス形状を描画した TileSize × TileSize テクスチャを生成する。
        /// ・ヘックス外は透明（alpha=0）
        /// ・外周 2px 分はソフトエッジ（アンチエイリアス代わり）
        /// ・内側 1px は枠線（fill を暗くした色）
        /// </summary>
        private static Color[] DrawHexTile(int size, Color fill)
        {
            var pixels  = new Color[size * size];
            // 枠線色: fill を暗くする（アルゴリズム定数）
            var border  = new Color(fill.r * 0.45f, fill.g * 0.45f, fill.b * 0.45f, 1f);

            float cx     = size * 0.5f;
            float cy     = size * 0.5f;
            float outerR = size * 0.47f;   // ヘックス外接半径（ピクセル）
            float softW  = size * 0.025f;  // ソフトエッジの幅
            float borderW = size * 0.05f;  // 枠線の幅

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float px = x + 0.5f - cx;
                    float py = y + 0.5f - cy;

                    // 正規化した flat-top hex 距離（1.0 = 境界）
                    float dist = HexDistance(px / outerR, py / outerR);

                    if (dist >= 1f + softW / outerR)
                    {
                        // ヘックス外: 透明
                        pixels[y * size + x] = Color.clear;
                    }
                    else if (dist >= 1f - softW / outerR)
                    {
                        // ソフトエッジ: fillへフェード
                        float t = (dist - (1f - softW / outerR)) / (2f * softW / outerR);
                        float a = 1f - Mathf.Clamp01(t);
                        pixels[y * size + x] = new Color(fill.r, fill.g, fill.b, a);
                    }
                    else if (dist >= 1f - (softW + borderW) / outerR)
                    {
                        // 枠線帯
                        pixels[y * size + x] = border;
                    }
                    else
                    {
                        // 内部: fill
                        pixels[y * size + x] = fill;
                    }
                }
            }
            return pixels;
        }

        /// <summary>
        /// 正規化座標 (px, py) の flat-top hex 距離を返す（0=中心, 1=境界）。
        /// </summary>
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
