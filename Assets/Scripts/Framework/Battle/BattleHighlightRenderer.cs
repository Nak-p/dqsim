// Assets/Scripts/Framework/Battle/BattleHighlightRenderer.cs
// AgentSim — バトルハイライト描画
//
// sortingOrder:
//   3 = 移動後攻撃可能エリア（オレンジ）
//   4 = 移動範囲（前方=緑 / 後方=黄）
//   5 = アクティブユニット位置（黄色）/ 攻撃対象（赤）

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

        public static void SetParent(Transform parent) => _parent = parent;

        public static void Clear()
        {
            foreach (var go in _pool)
                if (go != null) Object.Destroy(go);
            _pool.Clear();
        }

        /// <summary>
        /// 移動範囲 + 攻撃可能エリアをまとめて表示する。
        /// frontReach  : 前方移動のみで到達可能（緑）
        /// rearReach   : 後方移動が必要（黄）
        /// attackHexes : 現在地または移動後に攻撃できる敵マス（オレンジ）
        /// </summary>
        public static void ShowMoveRange(
            HashSet<HexCoord> frontReach,
            HashSet<HexCoord> rearReach,
            HashSet<HexCoord> attackHexes,
            HexCoord activeHex,
            Tilemap hexTilemap)
        {
            Clear();
            var visual = SettingsRegistry.Current?.BattleVisual;

            // 色を JSON から取得（front/rear をより明確に区別する値）
            var frontColor  = GetColor(visual?.highlight_move_color,
                                       new Color(0.10f, 0.90f, 0.25f, 0.55f)); // 鮮やか緑
            var rearColor   = GetColor(visual?.highlight_rear_move_color,
                                       new Color(0.95f, 0.85f, 0.05f, 0.55f)); // 明確な黄
            var attackColor = GetColor(visual?.highlight_attack_color,
                                       new Color(0.95f, 0.40f, 0.05f, 0.65f)); // オレンジ
            var activeColor = GetColor(visual?.highlight_active_color,
                                       new Color(0.90f, 0.85f, 0.20f, 0.70f)); // 黄色

            // 攻撃可能エリア（最下層: 移動範囲より下）
            foreach (var hex in attackHexes)
                Spawn(hex, attackColor, hexTilemap, 3);

            // 後方移動マス
            foreach (var hex in rearReach)
                Spawn(hex, rearColor, hexTilemap, 4);

            // 前方移動マス（rearの上に重ねる）
            foreach (var hex in frontReach)
                Spawn(hex, frontColor, hexTilemap, 4);

            // アクティブユニット（最上層）
            Spawn(activeHex, activeColor, hexTilemap, 5);
        }

        public static void ShowActionTargets(
            HashSet<HexCoord> targets, HexCoord activeHex, Tilemap hexTilemap)
        {
            Clear();
            var visual = SettingsRegistry.Current?.BattleVisual;

            var attackColor = GetColor(visual?.highlight_attack_color,
                                       new Color(0.95f, 0.25f, 0.10f, 0.65f)); // 赤
            var activeColor = GetColor(visual?.highlight_active_color,
                                       new Color(0.90f, 0.85f, 0.20f, 0.70f));

            foreach (var hex in targets)
                Spawn(hex, attackColor, hexTilemap, 4);

            Spawn(activeHex, activeColor, hexTilemap, 5);
        }

        // ── 内部ヘルパー ───────────────────────────────────────────────
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
    }
}
