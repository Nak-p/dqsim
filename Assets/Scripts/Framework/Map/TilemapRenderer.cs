// Assets/Scripts/Framework/Map/TilemapRenderer.cs
// AgentSim — MapData を Unity Tilemap へ描画する MonoBehaviour
//
// タイルの色・パターンは TileTypeConfig（tile_types.json）から読み込む。
// C# に色定数・地形名をハードコーディングしてはいけない。
// パターン描画は framework の視覚アルゴリズム（ゲームパラメータではない）。

using UnityEngine;
using UnityEngine.Tilemaps;
using AgentSim.Config;

namespace AgentSim.Map
{
    public class TilemapRenderer : MonoBehaviour
    {
        // ── Inspector 設定 ────────────────────────────────────────────
        [Header("Tilemaps")]
        public Tilemap terrainTilemap;
        public Tilemap markerTilemap;

        // ── 内部キャッシュ ────────────────────────────────────────────
        private Tile[] _terrainTiles;  // tile_types 配列インデックスと対応
        private Tile   _baseTile;
        private Tile   _destTile;

        // ── Unity ライフサイクル ──────────────────────────────────────
        private void Awake()
        {
            // SettingsRegistry がロード済みであればタイルを構築する
            if (SettingsRegistry.Current?.TileTypes != null)
                BuildTiles();
        }

        // ── 公開 API ─────────────────────────────────────────────────
        /// <summary>設定（世界観）が切り替わったときにタイルを再構築する。</summary>
        public void RebuildTiles() => BuildTiles();

        /// <summary>MapData の内容を Tilemap へ描画し、マーカーを配置する。</summary>
        public void RenderMap(MapData map)
        {
            if (_terrainTiles == null) BuildTiles();
            if (_terrainTiles == null) return; // SettingsRegistry 未ロード

            terrainTilemap.ClearAllTiles();
            markerTilemap.ClearAllTiles();

            for (int x = 0; x < map.Width; x++)
                for (int y = 0; y < map.Height; y++)
                {
                    int idx = map.Tiles[x, y];
                    if (idx >= 0 && idx < _terrainTiles.Length && _terrainTiles[idx] != null)
                        terrainTilemap.SetTile(new Vector3Int(x, y, 0), _terrainTiles[idx]);
                }

            if (_baseTile != null)
                markerTilemap.SetTile(
                    new Vector3Int(map.BasePosition.x, map.BasePosition.y, 0), _baseTile);
            if (_destTile != null)
                markerTilemap.SetTile(
                    new Vector3Int(map.DestinationPosition.x, map.DestinationPosition.y, 0), _destTile);
        }

        // ── タイル構築 ────────────────────────────────────────────────
        private void BuildTiles()
        {
            var defs = SettingsRegistry.Current?.TileTypes?.tile_types;
            if (defs == null)
            {
                Debug.LogWarning("[TilemapRenderer] TileTypeConfig が見つかりません。SettingsRegistry を先にロードしてください。");
                return;
            }

            _terrainTiles = new Tile[defs.Length];

            for (int i = 0; i < defs.Length; i++)
            {
                var def  = defs[i];
                var tile = BuildTile(def);

                switch (def.generator_role)
                {
                    case "marker_base": _baseTile        = tile; break;
                    case "marker_dest": _destTile        = tile; break;
                    default:            _terrainTiles[i] = tile; break;
                }
            }
        }

        /// <summary>
        /// TileTypeDef の pattern / base_color / secondary_color から
        /// 手続き生成テクスチャ付きの Tile を生成する。
        /// </summary>
        private static Tile BuildTile(TileTypeDef def)
        {
            const int Size = 16;
            Color32 main = ToColor32(def.BaseColor);
            Color32 sub  = ToColor32(def.SecondaryColor);

            Color32[] pixels = def.pattern switch
            {
                "checkerboard" => DrawCheckerboard(Size, main, sub),
                "dotted"       => DrawDotted(Size, main, sub),
                "mountain"     => DrawMountain(Size, main, sub),
                "marker_base"  => DrawMarkerBase(Size, main, sub),
                "marker_dest"  => DrawMarkerDest(Size, main, sub),
                _              => DrawFlat(Size, main),           // "flat" or unknown
            };

            var tex = new Texture2D(Size, Size) { filterMode = FilterMode.Point };
            tex.SetPixels32(pixels);
            tex.Apply();

            var tile = ScriptableObject.CreateInstance<Tile>();
            tile.sprite = Sprite.Create(tex, new Rect(0, 0, Size, Size),
                                        new Vector2(0.5f, 0.5f), Size);
            return tile;
        }

        // ── パターン描画メソッド ───────────────────────────────────────
        private static Color32[] DrawFlat(int size, Color32 color)
        {
            var p = new Color32[size * size];
            for (int i = 0; i < p.Length; i++) p[i] = color;
            return p;
        }

        // 水・クーラント等: 2色チェッカーパターン
        private static Color32[] DrawCheckerboard(int size, Color32 main, Color32 sub)
        {
            var p = new Color32[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                    p[y * size + x] = ((x + y) % 4 == 0) ? sub : main;
            return p;
        }

        // 森・サーバークラスター等: ドット模様
        private static Color32[] DrawDotted(int size, Color32 main, Color32 sub)
        {
            var p = new Color32[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                    p[y * size + x] = (x % 4 == 2 && y % 4 == 2) ? sub : main;
            return p;
        }

        // 山・壁等: 三角ピーク形状
        private static Color32[] DrawMountain(int size, Color32 main, Color32 peak)
        {
            var p = new Color32[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    int cx = x - size / 2;
                    p[y * size + x] = (Mathf.Abs(cx) <= (size - 1 - y) / 2 && y > size / 4)
                        ? peak : main;
                }
            return p;
        }

        // 拠点マーカー: 白地に十字
        private static Color32[] DrawMarkerBase(int size, Color32 bg, Color32 cross)
        {
            var p      = new Color32[size * size];
            Color32 border = new Color32(0x44, 0x44, 0x44, 0xff); // 枠線（アルゴリズム定数）
            int lo = size * 6 / 16;
            int hi = size * 9 / 16;
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    bool isBorder = x == 0 || x == size - 1 || y == 0 || y == size - 1;
                    bool isCross  = (x >= lo && x <= hi) || (y >= lo && y <= hi);
                    p[y * size + x] = isBorder ? border : (isCross ? cross : bg);
                }
            return p;
        }

        // 目的地マーカー: 濃色地に縦横十字
        private static Color32[] DrawMarkerDest(int size, Color32 bg, Color32 star)
        {
            var p = new Color32[size * size];
            for (int i = 0; i < p.Length; i++) p[i] = bg;

            int mid = size / 2;
            // 縦棒
            for (int y = 2; y <= size - 3; y++)
            {
                p[y * size + mid - 1] = star;
                p[y * size + mid    ] = star;
            }
            // 横棒
            for (int x = 2; x <= size - 3; x++)
            {
                p[(mid - 1) * size + x] = star;
                p[ mid      * size + x] = star;
            }
            return p;
        }

        // ── 色変換ユーティリティ ──────────────────────────────────────
        private static Color32 ToColor32(Color c) =>
            new Color32((byte)(c.r * 255), (byte)(c.g * 255),
                        (byte)(c.b * 255), (byte)(c.a * 255));
    }
}
