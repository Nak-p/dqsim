using UnityEngine;
using UnityEngine.Tilemaps;
// namespace DQSim と同名の DQSim.TilemapRenderer があるため、Unity の TilemapRenderer は明示する
using U2D = UnityEngine.Tilemaps;

namespace DQSim.Battle
{
    /// <summary>BattleHexMap を Hexagonal Tilemap に描画する（ランタイム生成タイル）。</summary>
    public class BattleHexTilemapRenderer : MonoBehaviour
    {
        public Tilemap terrainTilemap;

        private Tile[] _terrainTiles;
        private bool _tilesBuilt;

        private void Awake() => EnsureTiles();

        /// <summary>
        /// URP 2D では Tilemap の既定が Lit になり、ライトが足りないとタイルが真っ黒になることがある。
        /// </summary>
        public static void ApplyUnlitMaterialForUrp2D(U2D.TilemapRenderer tmr)
        {
            if (tmr == null) return;
            // Unity 6000 では TilemapRenderer に material が直接なく Renderer 経由になる
            var rend = tmr.GetComponent<Renderer>();
            if (rend == null) return;

            foreach (var shaderName in UnlitShaderCandidates)
            {
                var s = Shader.Find(shaderName);
                if (s == null) continue;
                rend.material = new Material(s);
                return;
            }

#if UNITY_EDITOR
            Debug.LogWarning(
                "BattleHexTilemapRenderer: Unlit シェーダが見つかりません。タイルが黒く見える場合は URP 2D の設定を確認してください。");
#endif
        }

        private static readonly string[] UnlitShaderCandidates =
        {
            "Universal Render Pipeline/2D/Sprite-Unlit-Default",
            "Universal 2D/Sprite-Unlit-Default",
            "Universal Render Pipeline/Unlit",
            "Sprites/Default",
            "UI/Default",
        };

        public void RenderMap(BattleHexMap map)
        {
            EnsureTiles();

            if (terrainTilemap == null)
            {
                Debug.LogError("BattleHexTilemapRenderer: terrainTilemap is not assigned.");
                return;
            }

            if (_terrainTiles == null || _terrainTiles.Length == 0)
            {
                Debug.LogError("BattleHexTilemapRenderer: terrain tiles failed to build.");
                return;
            }

            // シーンに Tilemap だけ置いてあるケースでは Renderer が無く何も描画されない
            var tmr = terrainTilemap.GetComponent<U2D.TilemapRenderer>();
            if (tmr == null)
            {
                tmr = terrainTilemap.gameObject.AddComponent<U2D.TilemapRenderer>();
#if UNITY_EDITOR
                Debug.LogWarning(
                    "BattleHexTilemapRenderer: Terrain に TilemapRenderer が無かったため追加しました。" +
                    "エディタでは Terrain に TilemapRenderer を付けておくとよいです。");
#endif
            }

            terrainTilemap.color = Color.white;
            var rend = tmr.GetComponent<Renderer>();
            if (rend != null)
            {
                rend.sortingLayerID = SortingLayer.NameToID("Default");
                rend.sortingOrder = 100;
            }

            ApplyUnlitMaterialForUrp2D(tmr);

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

            terrainTilemap.CompressBounds();
            terrainTilemap.RefreshAllTiles();
        }

        private void EnsureTiles()
        {
            if (_tilesBuilt) return;
            BuildTiles();
            _tilesBuilt = true;
        }

        private void BuildTiles()
        {
            bool useHexSprites = UsesHexagonGridLayout();
            var hexLayout = useHexSprites ? ResolveHexLayout() : default;

            _terrainTiles = new Tile[5];
            if (useHexSprites)
            {
                _terrainTiles[(int)BattleTerrain.Plain] =
                    MakeHexFlatTile(hexLayout, new Color32(0x8e, 0xc4, 0x6b, 0xff));
                _terrainTiles[(int)BattleTerrain.Forest] = MakeHexForestTile(hexLayout);
                _terrainTiles[(int)BattleTerrain.Mountain] = MakeHexMountainTile(hexLayout);
                _terrainTiles[(int)BattleTerrain.Swamp] =
                    MakeHexFlatTile(hexLayout, new Color32(0x4a, 0x5d, 0x3a, 0xff));
                _terrainTiles[(int)BattleTerrain.Water] = MakeHexWaterTile(hexLayout);
            }
            else
            {
                _terrainTiles[(int)BattleTerrain.Plain] =
                    MakeFlatTile(16, new Color32(0x8e, 0xc4, 0x6b, 0xff));
                _terrainTiles[(int)BattleTerrain.Forest] = MakeForestTile();
                _terrainTiles[(int)BattleTerrain.Mountain] = MakeMountainTile();
                _terrainTiles[(int)BattleTerrain.Swamp] =
                    MakeFlatTile(16, new Color32(0x4a, 0x5d, 0x3a, 0xff));
                _terrainTiles[(int)BattleTerrain.Water] = MakeWaterTile();
            }
        }

        /// <summary>親 Grid が Hexagon のときは先細り六角スプライトを使う（矩形セルは従来の正方形タイル）。</summary>
        private bool UsesHexagonGridLayout()
        {
            if (terrainTilemap == null) return false;
            var grid = terrainTilemap.GetComponentInParent<Grid>();
            return grid != null && grid.cellLayout == GridLayout.CellLayout.Hexagon;
        }

        private struct HexLayout
        {
            public int TexW;
            public int TexH;
            public float Ppu;
            public float Cx;
            public float Cy;
            public float R;
        }

        /// <summary>
        /// 親 Grid の cellSize にスプライトのワールド寸法を一致させる。
        /// 半径は正六角がセルに内接する値に軽いオーバーラップのみ（Tight メッシュの継ぎ目用）。
        /// </summary>
        private HexLayout ResolveHexLayout()
        {
            const float ppu = 48f;
            const float radiusOverlap = 1.03f;

            var grid = terrainTilemap != null ? terrainTilemap.GetComponentInParent<Grid>() : null;
            Vector3 cs = grid != null ? grid.cellSize : new Vector3(0.8660254f, 1f, 1f);

            int texH = Mathf.Max(16, Mathf.RoundToInt(cs.y * ppu));
            int texW = Mathf.Max(16, Mathf.RoundToInt(cs.x * ppu));

            // ピクセル中心 (0..w-1) に合わせた幾何中心（texW/2 だと半ピクセルずれる）
            float cx = (texW - 1) * 0.5f;
            float cy = (texH - 1) * 0.5f;

            // 先細り正六角: 幅 sqrt(3)*R、高さ 2R がテクスチャに収まる最大の R
            float rFromHeight = (texH - 1) * 0.5f;
            float rFromWidth = (texW - 1) / Mathf.Sqrt(3f);
            float rInscribed = Mathf.Min(rFromHeight, rFromWidth);
            // (rInscribed - 1) などで引くと R が 0 付近になりスプライトが空になることがあるため禁止
            float r = Mathf.Max(2f, rInscribed * radiusOverlap);

            return new HexLayout
            {
                TexW = texW,
                TexH = texH,
                Ppu = ppu,
                Cx = cx,
                Cy = cy,
                R = r,
            };
        }

        /// <summary>
        /// 先細り（頂点が上下）の正六角形内部判定。外接半径 R（中心〜頂点）。
        /// y 増加はテクスチャ座標（Unity: 左下原点・上向き）に合わせる。
        /// 旧式 maxDx=√3/2·R·(1-dy/R) はこの向きの六角では誤り（ひし形に近い領域になる）。
        /// </summary>
        private static bool InsidePointyTopHex(float px, float py, float cx, float cy, float r)
        {
            const float eps = 1e-4f;
            var p = new Vector2(px, py);
            // 頂点は上から反時計回り（テクスチャ y 上向き）
            for (int i = 0; i < 6; i++)
            {
                float a1 = Mathf.PI / 2f + i * (Mathf.PI / 3f);
                float a2 = Mathf.PI / 2f + (i + 1) * (Mathf.PI / 3f);
                var v1 = new Vector2(cx + r * Mathf.Cos(a1), cy + r * Mathf.Sin(a1));
                var v2 = new Vector2(cx + r * Mathf.Cos(a2), cy + r * Mathf.Sin(a2));
                var edge = v2 - v1;
                var inward = p - v1;
                float cross = edge.x * inward.y - edge.y * inward.x;
                if (cross < -eps)
                    return false;
            }

            return true;
        }

        /// <summary>
        /// FullRect は常に「長方形の四角形」メッシュになり、見た目が正方セルになる。
        /// Tight はアルファ境界に沿ってメッシュを切り、六角形らしいシルエットになる。
        /// </summary>
        private static Tile TileFromHexTexture(Texture2D tex, float pixelsPerUnit)
        {
            var tile = ScriptableObject.CreateInstance<Tile>();
            tile.sprite = Sprite.Create(
                tex,
                new Rect(0, 0, tex.width, tex.height),
                new Vector2(0.5f, 0.5f),
                pixelsPerUnit,
                0,
                SpriteMeshType.Tight);
            return tile;
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

        private static Tile MakeHexFlatTile(HexLayout hl, Color32 color)
        {
            var tex = new Texture2D(hl.TexW, hl.TexH) { filterMode = FilterMode.Point };
            tex.alphaIsTransparency = true;
            var clear = new Color32(0, 0, 0, 0);
            var pixels = new Color32[hl.TexW * hl.TexH];
            for (int y = 0; y < hl.TexH; y++)
            {
                for (int x = 0; x < hl.TexW; x++)
                {
                    pixels[y * hl.TexW + x] =
                        InsidePointyTopHex(x + 0.5f, y + 0.5f, hl.Cx, hl.Cy, hl.R) ? color : clear;
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply();
            return TileFromHexTexture(tex, hl.Ppu);
        }

        private static Tile MakeHexWaterTile(HexLayout hl)
        {
            Color32 deep = new Color32(0x25, 0x63, 0xa8, 0xff);
            Color32 light = new Color32(0x4a, 0x90, 0xd9, 0xff);
            var tex = new Texture2D(hl.TexW, hl.TexH) { filterMode = FilterMode.Point };
            tex.alphaIsTransparency = true;
            var clear = new Color32(0, 0, 0, 0);
            var pixels = new Color32[hl.TexW * hl.TexH];
            for (int y = 0; y < hl.TexH; y++)
            {
                for (int x = 0; x < hl.TexW; x++)
                {
                    if (!InsidePointyTopHex(x + 0.5f, y + 0.5f, hl.Cx, hl.Cy, hl.R))
{
                        pixels[y * hl.TexW + x] = clear;
                        continue;
                    }

                    // 斜め縞だと菱形に見えることがあるのでチェックを軸沿いにする
                    pixels[y * hl.TexW + x] = (((x >> 2) ^ (y >> 2)) & 1) == 0 ? light : deep;
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply();
            return TileFromHexTexture(tex, hl.Ppu);
        }

        private static Tile MakeHexForestTile(HexLayout hl)
        {
            Color32 bg = new Color32(0x2d, 0x5e, 0x1e, 0xff);
            Color32 dot = new Color32(0x1a, 0x3d, 0x0f, 0xff);
            var tex = new Texture2D(hl.TexW, hl.TexH) { filterMode = FilterMode.Point };
            tex.alphaIsTransparency = true;
            var clear = new Color32(0, 0, 0, 0);
            var pixels = new Color32[hl.TexW * hl.TexH];
            int stride = Mathf.Max(2, hl.TexW / 6);
            for (int y = 0; y < hl.TexH; y++)
            {
                for (int x = 0; x < hl.TexW; x++)
                {
                    if (!InsidePointyTopHex(x + 0.5f, y + 0.5f, hl.Cx, hl.Cy, hl.R))
{
                        pixels[y * hl.TexW + x] = clear;
                        continue;
                    }

                    pixels[y * hl.TexW + x] =
                        (x % stride == stride / 2 && y % stride == stride / 2) ? dot : bg;
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply();
            return TileFromHexTexture(tex, hl.Ppu);
        }

        private static Tile MakeHexMountainTile(HexLayout hl)
        {
            Color32 bg = new Color32(0x8c, 0x7a, 0x6b, 0xff);
            Color32 peak = new Color32(0xcc, 0xbb, 0xaa, 0xff);
            var tex = new Texture2D(hl.TexW, hl.TexH) { filterMode = FilterMode.Point };
            tex.alphaIsTransparency = true;
            var clear = new Color32(0, 0, 0, 0);
            int mid = hl.TexW / 2;
            var pixels = new Color32[hl.TexW * hl.TexH];
            for (int y = 0; y < hl.TexH; y++)
            {
                for (int x = 0; x < hl.TexW; x++)
                {
                    if (!InsidePointyTopHex(x + 0.5f, y + 0.5f, hl.Cx, hl.Cy, hl.R))
{
                        pixels[y * hl.TexW + x] = clear;
                        continue;
                    }

                    int cxRel = x - mid;
                    pixels[y * hl.TexW + x] =
                        (Mathf.Abs(cxRel) <= (hl.TexH - 1 - y) / 2 && y > hl.TexH / 4) ? peak : bg;
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply();
            return TileFromHexTexture(tex, hl.Ppu);
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
