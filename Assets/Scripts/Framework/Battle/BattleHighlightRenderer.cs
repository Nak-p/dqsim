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

            var rangeColor  = new Color(0.90f, 0.15f, 0.10f, 0.35f);   // 射程内空マス（薄赤）
            var targetColor = GetColor(visual?.highlight_attack_color,
                                       new Color(0.95f, 0.20f, 0.10f, 0.70f)); // 攻撃可能マス（濃赤）
            var activeColor = GetColor(visual?.highlight_active_color,
                                       new Color(0.90f, 0.85f, 0.20f, 0.70f));

            foreach (var hex in rangeHexes)
                Spawn(hex, rangeColor, hexTilemap, 3);

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

            var dirs = HexCoord.AllDirections;

            for (int d = 0; d < dirs.Length; d++)
            {
                bool isFront = BattleMovement.IsFrontDirection(facing, d);

                // 隣接セルのワールド座標（GetCellCenterWorld で正確な位置を取得）
                var nbHex   = new HexCoord(activeHex.Q + dirs[d].Q, activeHex.R + dirs[d].R);
                var nbCell  = BattleTilemapRenderer.HexToTilemapPos(nbHex);
                var nbWorld = hexTilemap.GetCellCenterWorld(nbCell);
                nbWorld.z   = 0f;

                // 実際の隣接距離（tileSpan 計算の代わりに実測値を使用）
                var diffVec = nbWorld - centerWorld;
                float interHexDist = diffVec.magnitude;
                if (interHexDist < 0.001f) continue;   // 距離ゼロガード
                var  dirN = diffVec / interHexDist;

                // 三角形を中心 → 隣接 の 42% 位置に配置（ユニットのセル内に収める）
                var arrowPos = centerWorld + dirN * interHexDist * 0.42f;

                // 色（前方=緑 / 後方=黄）
                var color = isFront
                    ? new Color(0.10f, 0.90f, 0.25f, 0.90f)
                    : new Color(0.95f, 0.85f, 0.05f, 0.90f);

                // 隣接マスへのワールド角度で回転
                float angleDeg = Mathf.Atan2(dirN.y, dirN.x) * Mathf.Rad2Deg;

                var go = new GameObject($"DirArrow_{d}");
                if (_parent != null) go.transform.SetParent(_parent, false);
                go.transform.position = arrowPos;
                go.transform.rotation = Quaternion.Euler(0f, 0f, angleDeg);

                // スケール: 実測隣接距離の 22% × 18% (正方形に近いスケールで歪み防止)
                float arrowW = interHexDist * 0.22f;
                float arrowH = interHexDist * 0.18f;
                go.transform.localScale = new Vector3(arrowW, arrowH, 1f);

                var sr          = go.AddComponent<SpriteRenderer>();
                sr.sprite       = BuildTriangleSprite(color);
                sr.sortingOrder = 6;

                _pool.Add(go);

                // クリック判定用（配置位置を記録）
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

            var centerCell  = BattleTilemapRenderer.HexToTilemapPos(activeHex);
            var centerWorld = hexTilemap.GetCellCenterWorld(centerCell);
            centerWorld.z = 0f;

            // 隣接距離を実測（d=0 の隣接セルを使う）
            var dirs = HexCoord.AllDirections;
            var nb0   = new HexCoord(activeHex.Q + dirs[0].Q, activeHex.R + dirs[0].R);
            var nb0W  = hexTilemap.GetCellCenterWorld(
                            BattleTilemapRenderer.HexToTilemapPos(nb0));
            nb0W.z = 0f;
            float interHexDist = Mathf.Max((nb0W - centerWorld).magnitude, 0.01f);

            // ヒット半径 = 隣接距離の 28%
            float hitR = interHexDist * 0.28f;

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

        /// <summary>頂点が +X 方向を向く三角形スプライト。pivot = (0.5, 0.5)。</summary>
        private static Sprite BuildTriangleSprite(Color color)
        {
            var pixels = DrawTriangle(TexSize, color);
            var tex    = new Texture2D(TexSize, TexSize, TextureFormat.RGBA32, false)
                         { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            tex.SetPixels(pixels);
            tex.Apply();
            return Sprite.Create(tex,
                new Rect(0, 0, TexSize, TexSize),
                new Vector2(0.5f, 0.5f), TexSize);
        }

        /// <summary>
        /// 頂点が右（+X）の等辺三角形。
        /// apex(+0.78) = 右頂点、-apex = 左底辺。
        /// </summary>
        private static Color[] DrawTriangle(int size, Color fill)
        {
            var pixels = new Color[size * size];
            float cx = size * 0.5f, cy = size * 0.5f;
            const float apex = 0.78f;
            const float soft = 0.06f;

            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float px = (x + 0.5f - cx) / (size * 0.5f);
                float py = (y + 0.5f - cy) / (size * 0.5f);

                float halfH = (apex - px) / (2f * apex) * 0.88f;
                float absPy = Mathf.Abs(py);
                float edge  = halfH - absPy;

                Color c;
                if (px > apex || px < -apex || halfH <= 0f)
                    c = Color.clear;
                else if (edge < soft)
                {
                    float alpha = Mathf.Clamp01(edge / soft);
                    c = new Color(fill.r, fill.g, fill.b, fill.a * alpha);
                }
                else
                    c = fill;

                pixels[y * size + x] = c;
            }
            return pixels;
        }
    }
}
