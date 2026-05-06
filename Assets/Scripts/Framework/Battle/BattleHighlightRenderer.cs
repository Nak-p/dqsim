// Assets/Scripts/Framework/Battle/BattleHighlightRenderer.cs
// AgentSim — バトルハイライト描画
//
// sortingOrder:
//   3 = 攻撃可能エリア（赤）
//   4 = 移動範囲（前方=緑 / 後方=黄）
//   5 = アクティブユニット位置（黄色）/ 攻撃対象（赤）
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

        // 三角形クリック半径（タイル半径の 40%）
        private const float ArrowHitFraction = 0.40f;

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
        /// <summary>
        /// frontReach  : 前方移動のみで到達可能（緑）
        /// rearReach   : 後方移動が必要（黄）
        /// attackHexes : 現在地から攻撃できる敵マス（赤）
        /// activeHex   : アクティブユニット位置
        /// facing      : アクティブユニットの向き (0-5)
        /// </summary>
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
            var attackColor = new Color(0.90f, 0.15f, 0.10f, 0.60f); // 赤
            var activeColor = GetColor(visual?.highlight_active_color,
                                       new Color(0.90f, 0.85f, 0.20f, 0.70f));

            // 攻撃可能エリア（最下層）
            foreach (var hex in attackHexes)
                Spawn(hex, attackColor, hexTilemap, 3);

            // 後方移動マス
            foreach (var hex in rearReach)
                Spawn(hex, rearColor, hexTilemap, 4);

            // 前方移動マス
            foreach (var hex in frontReach)
                Spawn(hex, frontColor, hexTilemap, 4);

            // アクティブユニット（最上層）
            Spawn(activeHex, activeColor, hexTilemap, 5);

            // 方向三角形（6方向）
            SpawnDirectionArrows(activeHex, facing, hexTilemap);
        }

        public static void ShowActionTargets(
            HashSet<HexCoord> targets, HexCoord activeHex, Tilemap hexTilemap)
        {
            Clear();
            var visual = SettingsRegistry.Current?.BattleVisual;

            var attackColor = GetColor(visual?.highlight_attack_color,
                                       new Color(0.95f, 0.25f, 0.10f, 0.65f));
            var activeColor = GetColor(visual?.highlight_active_color,
                                       new Color(0.90f, 0.85f, 0.20f, 0.70f));

            foreach (var hex in targets)
                Spawn(hex, attackColor, hexTilemap, 4);

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
            centerWorld.z   = 0f;

            var cellSize  = hexTilemap.layoutGrid.cellSize;
            float tileR   = (cellSize.x + cellSize.y) * 0.5f * 0.5f; // おおよそのタイル半径

            var dirs = HexCoord.AllDirections;

            for (int d = 0; d < dirs.Length; d++)
            {
                bool isFront = BattleMovement.IsFrontDirection(facing, d);

                // 隣接セル中心のワールド座標
                var nbHex   = new HexCoord(activeHex.Q + dirs[d].Q, activeHex.R + dirs[d].R);
                var nbCell  = BattleTilemapRenderer.HexToTilemapPos(nbHex);
                var nbWorld = hexTilemap.GetCellCenterWorld(nbCell);
                nbWorld.z   = 0f;

                // 三角形を中心と隣接セルの中間に配置
                var arrowPos = (centerWorld + nbWorld) * 0.5f;

                // 色（前方=緑 / 後方=黄）
                var color = isFront
                    ? new Color(0.10f, 0.90f, 0.25f, 0.85f)
                    : new Color(0.95f, 0.85f, 0.05f, 0.85f);

                // 中心 → 隣接 方向の角度（Z 軸回転）
                var diff = nbWorld - centerWorld;
                float angleDeg = Mathf.Atan2(diff.y, diff.x) * Mathf.Rad2Deg;

                // 三角形スプライト生成（頂点が +X 方向 → angleDeg だけ回転）
                var go = new GameObject($"DirArrow_{d}");
                if (_parent != null) go.transform.SetParent(_parent, false);
                go.transform.position = arrowPos;
                go.transform.rotation = Quaternion.Euler(0f, 0f, angleDeg);
                go.transform.localScale = new Vector3(tileR * 0.70f, tileR * 0.55f, 1f);

                var sr          = go.AddComponent<SpriteRenderer>();
                sr.sprite       = BuildTriangleSprite(color);
                sr.sortingOrder = 6;

                _pool.Add(go);

                // クリック判定用に位置を記録
                _dirArrowPositions[d] = arrowPos;
            }
        }

        // ─────────────────────────────────────────────────────────────
        // クリック座標が三角形に当たっているか判定
        // ─────────────────────────────────────────────────────────────
        /// <summary>
        /// worldPos がいずれかの方向三角形に当たっていれば dirIndex を返す。
        /// hitRadius はタイル間距離の ArrowHitFraction 倍。
        /// </summary>
        public static bool TryGetClickedDirection(
            Vector3 worldPos, Tilemap hexTilemap, HexCoord activeHex, out int dirIndex)
        {
            dirIndex = -1;
            if (_dirArrowPositions.Count == 0) return false;

            var cellSize  = hexTilemap.layoutGrid.cellSize;
            float tileR   = (cellSize.x + cellSize.y) * 0.5f * 0.5f;
            float hitR    = tileR * ArrowHitFraction * 2f; // 実際の判定半径

            float best = float.MaxValue;
            foreach (var kv in _dirArrowPositions)
            {
                float dist = Vector3.Distance(worldPos, kv.Value);
                if (dist < hitR && dist < best)
                {
                    best = dist;
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

        /// <summary>右向き（+X）の三角形テクスチャ。SpriteRenderer の rotation で向きを調整。</summary>
        private static Sprite BuildTriangleSprite(Color color)
        {
            var pixels = DrawTriangle(TexSize, color);
            var tex    = new Texture2D(TexSize, TexSize, TextureFormat.RGBA32, false)
                         { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            tex.SetPixels(pixels);
            tex.Apply();
            return Sprite.Create(tex,
                new Rect(0, 0, TexSize, TexSize),
                new Vector2(0.35f, 0.5f), TexSize);  // pivot を少し左（底辺側）にずらす
        }

        /// <summary>頂点が右 (+X) を向く等辺三角形を塗りつぶす。</summary>
        private static Color[] DrawTriangle(int size, Color fill)
        {
            var pixels = new Color[size * size];
            float cx = size * 0.5f, cy = size * 0.5f;

            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float px = (x + 0.5f - cx) / (size * 0.5f); // -1..1
                float py = (y + 0.5f - cy) / (size * 0.5f); // -1..1

                // 右向き三角形: 頂点(1,0)、底辺 x=-0.7 の位置
                // 条件: x > -0.7 && |y| < (x + 0.7) / 1.7 * 1.0
                float threshold = 0.70f;
                bool inside = px > -threshold
                    && Mathf.Abs(py) < (px + threshold) / (1f + threshold) * 0.92f;

                if (inside)
                {
                    // ソフトエッジ
                    float edgeDist = (px + threshold) / (1f + threshold) * 0.92f - Mathf.Abs(py);
                    float alpha = Mathf.Clamp01(edgeDist / 0.08f);
                    pixels[y * size + x] = new Color(fill.r, fill.g, fill.b, fill.a * alpha);
                }
                else
                {
                    pixels[y * size + x] = Color.clear;
                }
            }
            return pixels;
        }
    }
}
