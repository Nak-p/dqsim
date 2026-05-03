// Assets/Scripts/Framework/Battle/BattleUnitRenderer.cs
// AgentSim — バトルグリッド上のユニットアイコン描画 MonoBehaviour
//
// ユニット色は BattleVisualConfig (battle_visual.json) から読み込む。
// C# に色定数をハードコーディングしてはいけない。
// アイコン生成アルゴリズム（形状・サイズ）は視覚定数として許可される。

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using AgentSim.Config;

namespace AgentSim.Battle
{
    public class BattleUnitRenderer : MonoBehaviour
    {
        // ── 内部状態 ──────────────────────────────────────────────────
        private Tilemap                          _tilemap;
        private Dictionary<string, GameObject>   _unitObjects; // AgentId → GameObject

        // アイコンサイズ（ピクセル）— アルゴリズム定数
        private const int IconSize = 12;

        // ── 初期化 ────────────────────────────────────────────────────
        public void Initialize(Tilemap tilemap)
        {
            _tilemap     = tilemap;
            _unitObjects = new Dictionary<string, GameObject>();
        }

        // ── ユニット操作 ──────────────────────────────────────────────
        /// <summary>ユニットのアイコンをグリッド上に配置する。</summary>
        public void PlaceUnit(BattleUnit unit)
        {
            if (_unitObjects.ContainsKey(unit.AgentId)) return;

            var color  = GetUnitColor(unit.Team);
            var sprite = CreateUnitSprite(color);

            var go = new GameObject($"Unit_{unit.AgentName}");
            go.transform.SetParent(transform, false);
            go.transform.position = GetWorldPos(unit.Position);

            var sr           = go.AddComponent<SpriteRenderer>();
            sr.sprite        = sprite;
            sr.sortingOrder  = 10;
            // タイルより少し小さく表示（アイコンがタイルの 60% サイズ）
            go.transform.localScale = new Vector3(0.6f, 0.6f, 1f);

            _unitObjects[unit.AgentId] = go;
        }

        /// <summary>ユニットアイコンを新しいタイルへ移動する。</summary>
        public void MoveUnit(BattleUnit unit, HexCoord to)
        {
            if (!_unitObjects.TryGetValue(unit.AgentId, out var go)) return;
            go.transform.position = GetWorldPos(to);
        }

        /// <summary>ユニットアイコンを削除する。</summary>
        public void RemoveUnit(BattleUnit unit)
        {
            if (!_unitObjects.TryGetValue(unit.AgentId, out var go)) return;
            Destroy(go);
            _unitObjects.Remove(unit.AgentId);
        }

        // ── 内部ユーティリティ ────────────────────────────────────────
        private Vector3 GetWorldPos(HexCoord hex)
        {
            var tilemapPos = BattleTilemapRenderer.HexToTilemapPos(hex);
            return _tilemap.GetCellCenterWorld(tilemapPos);
        }

        private Color32 GetUnitColor(BattleTeam team)
        {
            var visual = SettingsRegistry.Current?.BattleVisual;
            float[] raw = team == BattleTeam.Player
                ? visual?.player_unit_color
                : visual?.enemy_unit_color;

            if (raw != null && raw.Length >= 4)
                return new Color32(
                    (byte)(raw[0] * 255),
                    (byte)(raw[1] * 255),
                    (byte)(raw[2] * 255),
                    (byte)(raw[3] * 255));

            // フォールバック: プレイヤー=青、敵=赤
            return team == BattleTeam.Player
                ? new Color32(0x60, 0xa0, 0xff, 0xff)
                : new Color32(0xff, 0x60, 0x60, 0xff);
        }

        /// <summary>
        /// 指定色のユニットアイコンスプライトを手続き生成する。
        /// PartyController のアイコン生成と同じパターン。
        /// </summary>
        private static Sprite CreateUnitSprite(Color32 body)
        {
            var tex    = new Texture2D(IconSize, IconSize) { filterMode = FilterMode.Point };
            var pixels = new Color32[IconSize * IconSize];
            var bg     = new Color32(0, 0, 0, 0);
            var dark   = new Color32(
                (byte)(body.r / 2),
                (byte)(body.g / 2),
                (byte)(body.b / 2),
                0xff);

            for (int i = 0; i < pixels.Length; i++) pixels[i] = bg;

            SetRect(pixels, IconSize, 4,  9, 7, 11, body);
            SetRect(pixels, IconSize, 3,  5, 8,  8, body);
            SetRect(pixels, IconSize, 1,  5, 2,  7, body);
            SetRect(pixels, IconSize, 9,  5, 10, 7, body);
            SetRect(pixels, IconSize, 3,  2, 5,  4, body);
            SetRect(pixels, IconSize, 6,  2, 8,  4, body);
            SetRect(pixels, IconSize, 4,  9, 7,  9, dark);

            tex.SetPixels32(pixels);
            tex.Apply();
            return Sprite.Create(tex,
                new Rect(0, 0, IconSize, IconSize),
                new Vector2(0.5f, 0.5f),
                IconSize);
        }

        private static void SetRect(Color32[] pixels, int size,
            int x0, int y0, int x1, int y1, Color32 color)
        {
            for (int y = y0; y <= y1; y++)
                for (int x = x0; x <= x1; x++)
                    if (x >= 0 && x < size && y >= 0 && y < size)
                        pixels[y * size + x] = color;
        }
    }
}
