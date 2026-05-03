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
        private const int TileSize = 16;

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
            // フォールバック: タイル種別に応じたグレースケール
            return tileId switch
            {
                "player_spawn" => new Color(0.2f, 0.3f, 0.5f),
                "enemy_spawn"  => new Color(0.5f, 0.2f, 0.2f),
                _              => new Color(0.25f, 0.25f, 0.3f),
            };
        }

        private static Tile BuildTile(Color color)
        {
            var pixels = DrawHexTile(TileSize, ToColor32(color));
            var tex    = new Texture2D(TileSize, TileSize) { filterMode = FilterMode.Point };
            tex.SetPixels32(pixels);
            tex.Apply();

            var tile   = ScriptableObject.CreateInstance<Tile>();
            tile.sprite = Sprite.Create(tex,
                new Rect(0, 0, TileSize, TileSize),
                new Vector2(0.5f, 0.5f),
                TileSize);
            return tile;
        }

        /// <summary>
        /// ヘックス形状を描画した 16×16 テクスチャを生成する。
        /// ヘックス外は透明（alpha=0）にし、枠線で各セルを視覚的に分離する。
        /// </summary>
        private static Color32[] DrawHexTile(int size, Color32 fill)
        {
            var pixels   = new Color32[size * size];
            var transparent = new Color32(0, 0, 0, 0);
            // 枠線色: fill を暗くしたアルゴリズム定数
            var border   = new Color32(
                (byte)(fill.r / 2),
                (byte)(fill.g / 2),
                (byte)(fill.b / 2),
                0xff);

            float cx = size * 0.5f;
            float cy = size * 0.5f;
            // flat-top hex の外接円半径
            float outerR = size * 0.48f;
            // 枠線幅（内側へ）
            float innerR = outerR - 1.5f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float px = x + 0.5f - cx;
                    float py = y + 0.5f - cy;

                    // キューブ座標距離でヘックス内判定（flat-top hex）
                    // Chebyshev: max(|q|, |r|, |s|) <= r  を軸方向で近似
                    // より正確な判定: hex距離 <= 1 を正規化座標で計算
                    float hexDist = HexDistance(px / outerR, py / outerR);
                    if (hexDist > 1.0f)
                    {
                        pixels[y * size + x] = transparent;
                    }
                    else if (hexDist > innerR / outerR)
                    {
                        pixels[y * size + x] = border;
                    }
                    else
                    {
                        pixels[y * size + x] = fill;
                    }
                }
            }
            return pixels;
        }

        /// <summary>
        /// 正規化座標 (px, py) が flat-top hex 内かどうかの距離（0=中心, 1=境界）。
        /// </summary>
        private static float HexDistance(float px, float py)
        {
            // flat-top hexagon の半径1の境界判定
            // |px| <= 1, |px * 0.5 + py * sqrt3/2| <= 1, |px * 0.5 - py * sqrt3/2| <= 1
            const float Sqrt3Over2 = 0.8660254f;
            float a = Mathf.Abs(px);
            float b = Mathf.Abs(px * 0.5f + py * Sqrt3Over2);
            float c = Mathf.Abs(px * 0.5f - py * Sqrt3Over2);
            return Mathf.Max(a, Mathf.Max(b, c));
        }

        private static Color32 ToColor32(Color c) =>
            new Color32(
                (byte)(c.r * 255),
                (byte)(c.g * 255),
                (byte)(c.b * 255),
                (byte)(c.a * 255));
    }
}
