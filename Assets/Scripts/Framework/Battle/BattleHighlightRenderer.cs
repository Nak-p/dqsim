// Assets/Scripts/Framework/Battle/BattleHighlightRenderer.cs
// AgentSim — バトルハイライト描画
//
// sortingOrder:
//   3 = 攻撃可能エリア（赤）
//   4 = 移動範囲（前方=緑 / 後方=黄）
//   5 = アクティブユニット位置
//   6 = 向き三角形（前方=緑 / 後方=黄）

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using AgentSim.Config;

namespace AgentSim.Battle
{
    public static class BattleHighlightRenderer
    {
        private static readonly List<GameObject> _pool = new List<GameObject>();
        private static Transform _parent;
        private const int TexSize = 64;

        // 各方向インデックス → ワールド座標（三角形クリック判定用）
        private static readonly Dictionary<int, Vector3> _dirArrowPositions
            = new Dictionary<int, Vector3>();

        public static void SetParent(Transform parent) => _parent = parent;

        public static void Clear()
        {
            foreach (var go in _pool)
                if (go != null) Object.Destroy(go);
            _pool.Clear();
            _dirArrowPositions.Clear();
        }

        // ─────────────────────────────────────────────────────────────
        // 移動範囲 + 方向三角形 + 攻撃可能エリアをまとめて表示
        // ─────────────────────────────────────────────────────────────
        public static void ShowMoveRange(
            HashSet<HexCoord> frontReach,
            HashSet<HexCoord> rearReach,
            HashSet<HexCoord> attackHexes,
            HexCoord activeHex,
            int facing,
            Tilemap hexTilemap)
        {
            Clear();
            var visual = SettingsRegistry.Current?.BattleVisual;

            var frontColor  = GetColor(visual?.highlight_move_color,
                                       new Color(0.10f, 0.90f, 0.25f, 0.55f));
            var rearColor   = GetColor(visual?.highlight_rear_move_color,
                                       new Color(0.95f, 0.85f, 0.05f, 0.55f));
            var attackColor = new Color(0.90f, 0.15f, 0.10f, 0.60f);
            var activeColor = GetColor(visual?.highlight_active_color,
                                       new Color(0.90f, 0.85f, 0.20f, 0.70f));

            foreach (var hex in attackHexes)
                Spawn(hex, attackColor, hexTilemap, 3);

            foreach (var hex in rearReach)
                Spawn(hex, rearColor, hexTilemap, 4);

            foreach (var hex in frontReach)
                Spawn(hex, frontColor, hexTilemap, 4);

            Spawn(activeHex, activeColor, hexTilemap, 5);

            SpawnDirectionArrows(activeHex, facing, hexTilemap);
        }

        public static void ShowActionTargets(
            HashSet<HexCoord> rangeHexes,
            HashSet<HexCoord> validTargets,
            HexCoord activeHex,
            Tilemap hexTilemap)
        {
            Clear();
            var visual = SettingsRegistry.Current?.BattleVisual;

            // 射程内の全マスを薄赤、実際に攻撃できるマスを濃赤
            var rangeColor  = new Color(0.90f, 0.15f, 0.10f, 0.35f);
            var targetColor = GetColor(visual?.highlight_attack_color,
                                       new Color(0.95f, 0.20f, 0.10f, 0.70f));
            var activeColor = GetColor(visual?.highlight_active_color,
                                       new Color(0.90f, 0.85f, 0.20f, 0.70f));

            // 射程内の全マス（薄赤）
            foreach (var hex in rangeHexes)
                Spawn(hex, rangeColor, hexTilemap, 3);

            // 実際のターゲットマス（濃赤・上に重ねる）
            foreach (var hex in validTargets)
                Spawn(hex, targetColor, hexTilemap, 4);

            Spawn(activeHex, activeColor, hexTilemap, 5);
        }

        // ─────────────────────────────────────────────────────────────
        // 方向三角形 — 6方向にスポーン
        // ─────────────────────────────────────────────────────────────
        private static void SpawnDirectionArrows(
            HexCoord activeHex, int facing, Tilemap hexTilemap)
        {
            var centerCell  = BattleTilemapRenderer.HexToTilemapPos(activeHex);
            var centerWorld = hexTilemap.GetCellCenterWorld(centerCell);
            centerWorld.z = 0f;

            var cellSize = hexTilemap.layoutGrid.cellSize;
            // タイル間距離の目安（六角形のセンター間距離）
            float tileSpan = (cellSize.x + cellSize.y) * 0.5f;

            var dirs = HexCoord.AllDirections;

            for (int d = 0; d < dirs.Length; d++)
            {
                bool isFront = BattleMovement.IsFrontDirection(facing, d);

                // 隣接セル中心のワールド座標
                var nbHex   = new HexCoord(activeHex.Q + dirs[d].Q, activeHex.R + dirs[d].R);
                var nbCell  = BattleTilemapRenderer.HexToTilemapPos(nbHex);
                var nbWorld = hexTilemap.GetCellCenterWorld(nbCell);
                nbWorld.z = 0f;

                // 方向ベクトルと中間点
                var dir     = nbWorld - centerWorld;
                float dist  = dir.magnitude;
                var  dirN   = dir / dist;

                // 三角形を中心 → 隣接 の 60% の位置に配置（ユニット側に寄せる）
                var arrowPos = centerWorld + dirN * dist * 0.55f;

                // 色（前方=緑 / 後方=黄）
                var color = isFront
                    ? new Color(0.10f, 0.90f, 0.25f, 0.90f)
                    : new Color(0.95f, 0.85f, 0.05f, 0.90f);

                // 頂点が隣接方向を指すようにする
                // atan2 は +X 基準なので、そのまま rotation に使える
                float angleDeg = Mathf.Atan2(dirN.y, dirN.x) * Mathf.Rad2Deg;

                var go = new GameObject($"DirArrow_{d}");
                if (_parent != null) go.transform.SetParent(_parent, false);
                go.transform.position = arrowPos;
                go.transform.rotation = Quaternion.Euler(0f, 0f, angleDeg);

                // スケール: タイル間距離の 30% × 25%
                float w = tileSpan * 0.30f;
                float h = tileSpan * 0.25f;
                go.transform.localScale = new Vector3(w, h, 1f);

                var sr          = go.AddComponent<SpriteRenderer>();
                sr.sprite       = BuildTriangleSprite(color);
                sr.sortingOrder = 6;

                _pool.Add(go);

                // クリック判定用位置（配置位置を記録）
                _dirArrowPositions[d] = arrowPos;
            }
        }

        // ─────────────────────────────────────────────────────────────
        // 方向三角形クリック判定
        // ─────────────────────────────────────────────────────────────
        public static bool TryGetClickedDirection(
            Vector3 worldPos, Tilemap hexTilemap, HexCoord activeHex, out int dirIndex)
        {
            dirIndex = -1;
            if (_dirArrowPositions.Count == 0) return false;

            var cellSize = hexTilemap.layoutGrid.cellSize;
            float tileSpan = (cellSize.x + cellSize.y) * 0.5f;
            float hitR = tileSpan * 0.30f; // タイル間距離の 30% をヒット半径とする

            float best = float.MaxValue;
            foreach (var kv in _dirArrowPositions)
            {
                float d = Vector3.Distance(worldPos, kv.Value);
                if (d < hitR && d < best)
                {
                    best = d;
                    dirIndex = kv.Key;
                }
            }
            return dirIndex >= 0;
        }

        // ─────────────────────────────────────────────────────────────
        // 内部ヘルパー — 六角形タイル
        // ─────────────────────────────────────────────────────────────
        private static void Spawn(HexCoord hex, Color color, Tilemap hexTilemap, int sortOrder)
        {
            var cellPos  = BattleTilemapRenderer.HexToTilemapPos(hex);
            var worldPos = hexTilemap.GetCellCenterWorld(cellPos);
            worldPos.z = 0f;

            var go = new GameObject($"Hl_{hex.Q}_{hex.R}_{sortOrder}");
            if (_parent != null) go.transform.SetParent(_parent, false);
            go.transform.position = worldPos;

            var cellSize = hexTilemap.layoutGrid.cellSize;
            go.transform.localScale = new Vector3(cellSize.x, cellSize.y, 1f);

            var sr          = go.AddComponent<SpriteRenderer>();
            sr.sprite       = BuildHexSprite(color);
            sr.sortingOrder = sortOrder;

            _pool.Add(go);
        }

        private static Color GetColor(float[] rgba, Color fallback)
        {
            if (rgba != null && rgba.Length >= 4)
                return new Color(rgba[0], rgba[1], rgba[2], rgba[3]);
            return fallback;
        }

        // ─────────────────────────────────────────────────────────────
        // テクスチャ生成
        // ─────────────────────────────────────────────────────────────
        private static Sprite BuildHexSprite(Color color)
        {
            var pixels = DrawHexTile(TexSize, color);
            var tex    = new Texture2D(TexSize, TexSize, TextureFormat.RGBA32, false)
                         { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            tex.SetPixels(pixels);
            tex.Apply();
            return Sprite.Create(tex,
                new Rect(0, 0, TexSize, TexSize),
                new Vector2(0.5f, 0.5f), TexSize);
        }

        private static Color[] DrawHexTile(int size, Color fill)
        {
            var pixels = new Color[size * size];
            float cx = size * 0.5f, cy = size * 0.5f;
            float outerR = size * 0.47f, softW = size * 0.025f;
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float px   = x + 0.5f - cx;
                float py   = y + 0.5f - cy;
                float dist = HexDist(py / outerR, px / outerR);
                if (dist >= 1f + softW / outerR)
                    pixels[y * size + x] = Color.clear;
                else if (dist >= 1f - softW / outerR)
                {
                    float t = (dist - (1f - softW / outerR)) / (2f * softW / outerR);
                    pixels[y * size + x] = new Color(fill.r, fill.g, fill.b,
                        fill.a * (1f - Mathf.Clamp01(t)));
                }
                else
                    pixels[y * size + x] = fill;
            }
            return pixels;
        }

        private static float HexDist(float px, float py)
        {
            const float Sqrt3Over2 = 0.8660254f;
            float a = Mathf.Abs(px);
            float b = Mathf.Abs(px * 0.5f + py * Sqrt3Over2);
            float c = Mathf.Abs(px * 0.5f - py * Sqrt3Over2);
            return Mathf.Max(a, Mathf.Max(b, c));
        }

        /// <summary>
        /// 頂点が +X 方向を向く三角形スプライト。
        /// SpriteRenderer の rotation で向きを合わせる。
        /// pivot = (0.5, 0.5) = テクスチャ中央。
        /// </summary>
        private static Sprite BuildTriangleSprite(Color color)
        {
            var pixels = DrawTriangle(TexSize, color);
            var tex    = new Texture2D(TexSize, TexSize, TextureFormat.RGBA32, false)
                         { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            tex.SetPixels(pixels);
            tex.Apply();
            // pivot をテクスチャ中央に設定（localScale で大きさ調整）
            return Sprite.Create(tex,
                new Rect(0, 0, TexSize, TexSize),
                new Vector2(0.5f, 0.5f), TexSize);
        }

        /// <summary>
        /// 頂点が右（+X）にある等辺三角形を描画。
        ///   px = -apex : 底辺（左端、幅が最大）
        ///   px = +apex : 頂点（右端、幅ゼロ）
        /// </summary>
        private static Color[] DrawTriangle(int size, Color fill)
        {
            var pixels = new Color[size * size];
            float cx = size * 0.5f, cy = size * 0.5f;
            const float apex  = 0.78f;  // 頂点の x 座標（+X 側）
            const float soft  = 0.06f;  // ソフトエッジ幅

            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                // 正規化座標 [-1, 1]
                float px = (x + 0.5f - cx) / (size * 0.5f);
                float py = (y + 0.5f - cy) / (size * 0.5f);

                // 右向き三角形: 頂点が +apex、底辺が -apex
                // |py| < (apex - px) / (2 * apex) * height
                float halfH = (apex - px) / (2f * apex) * 0.88f;

                float absPy = Mathf.Abs(py);
                float edge  = halfH - absPy;

                Color c;
                if (px > apex || px < -apex || halfH <= 0f)
                {
                    c = Color.clear;
                }
                else if (edge < soft)
                {
                    float alpha = Mathf.Clamp01(edge / soft);
                    c = new Color(fill.r, fill.g, fill.b, fill.a * alpha);
                }
                else
                {
                    c = fill;
                }
                pixels[y * size + x] = c;
            }
            return pixels;
        }
    }
}
