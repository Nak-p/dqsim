// Assets/Scripts/Framework/Battle/BattleHighlightRenderer.cs
// AgentSim — バトルハイライトオーバーレイの描画（Unity 依存、静的ユーティリティ）
//
// BattleTilemapRenderer と同じ BuildTile パターンを使い、
// 半透明タイルを別 Tilemap レイヤーに書き込む。
// 色は BattleVisualConfig (battle_visual.json) から取得する。

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using AgentSim.Config;

namespace AgentSim.Battle
{
    public static class BattleHighlightRenderer
    {
        private const int TileSize = 64;

        public static void Clear(Tilemap overlay)
        {
            overlay.ClearAllTiles();
        }

        public static void ShowMoveRange(
            HashSet<HexCoord> reachable, HexCoord activeHex, Tilemap overlay)
        {
            overlay.ClearAllTiles();
            var visual = SettingsRegistry.Current?.BattleVisual;

            var moveColor   = GetColor(visual?.highlight_move_color,   new Color(0.2f, 0.7f, 0.3f, 0.55f));
            var activeColor = GetColor(visual?.highlight_active_color, new Color(0.9f, 0.85f, 0.2f, 0.65f));

            foreach (var hex in reachable)
                overlay.SetTile(HexToTilemapPos(hex), BuildTile(moveColor));

            overlay.SetTile(HexToTilemapPos(activeHex), BuildTile(activeColor));
        }

        public static void ShowActionTargets(
            HashSet<HexCoord> targets, HexCoord activeHex, Tilemap overlay)
        {
            overlay.ClearAllTiles();
            var visual = SettingsRegistry.Current?.BattleVisual;

            var attackColor = GetColor(visual?.highlight_attack_color, new Color(0.8f, 0.2f, 0.2f, 0.55f));
            var activeColor = GetColor(visual?.highlight_active_color, new Color(0.9f, 0.85f, 0.2f, 0.65f));

            foreach (var hex in targets)
                overlay.SetTile(HexToTilemapPos(hex), BuildTile(attackColor));

            overlay.SetTile(HexToTilemapPos(activeHex), BuildTile(activeColor));
        }

        // ── 内部ヘルパー ───────────────────────────────────────────────
        private static Vector3Int HexToTilemapPos(HexCoord hex)
            => new Vector3Int(hex.Q, hex.R, 0);

        private static Color GetColor(float[] rgba, Color fallback)
        {
            if (rgba != null && rgba.Length >= 4)
                return new Color(rgba[0], rgba[1], rgba[2], rgba[3]);
            return fallback;
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
            var pixels = new Color[size * size];
            float cx = size * 0.5f, cy = size * 0.5f;
            float outerR = size * 0.47f, softW = size * 0.025f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float px = x + 0.5f - cx;
                    float py = y + 0.5f - cy;
                    float dist = HexDistance(py / outerR, px / outerR);

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
            }
            return pixels;
        }

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
