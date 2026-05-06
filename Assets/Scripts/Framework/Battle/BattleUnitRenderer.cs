// Assets/Scripts/Framework/Battle/BattleUnitRenderer.cs
// AgentSim — バトルグリッド上のユニットアイコン描画 MonoBehaviour

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using AgentSim.Config;

namespace AgentSim.Battle
{
    public class BattleUnitRenderer : MonoBehaviour
    {
        private Tilemap                        _tilemap;
        private Dictionary<string, GameObject> _unitObjects;

        private const int   IconSize     = 12;
        private const float MoveDuration = 0.28f;   // 移動アニメーション秒数

        // ── 初期化 ────────────────────────────────────────────────────
        public void Initialize(Tilemap tilemap)
        {
            _tilemap     = tilemap;
            _unitObjects = new Dictionary<string, GameObject>();
        }

        public void ClearAll()
        {
            if (_unitObjects != null)
            {
                foreach (var go in _unitObjects.Values)
                    if (go != null) DestroyImmediate(go);
                _unitObjects.Clear();
            }

            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                var child = transform.GetChild(i);
                if (child != null) DestroyImmediate(child.gameObject);
            }
        }

        // ── ユニット操作 ──────────────────────────────────────────────
        public void PlaceUnit(BattleUnit unit)
        {
            if (_unitObjects == null) _unitObjects = new Dictionary<string, GameObject>();
            if (_unitObjects.ContainsKey(unit.AgentId)) return;

            var color  = GetUnitColor(unit.Team);
            var sprite = CreateUnitSprite(color);

            var go = new GameObject($"Unit_{unit.AgentName}");
            go.transform.SetParent(transform, false);
            go.transform.position = GetWorldPos(unit.Position);

            var sr          = go.AddComponent<SpriteRenderer>();
            sr.sprite       = sprite;
            sr.sortingOrder = 10;
            go.transform.localScale = new Vector3(0.6f, 0.6f, 1f);

            _unitObjects[unit.AgentId] = go;
        }

        /// <summary>
        /// 即時移動（瞬間テレポート）。
        /// </summary>
        public void MoveUnit(BattleUnit unit, HexCoord to)
        {
            if (_unitObjects == null || !_unitObjects.TryGetValue(unit.AgentId, out var go)) return;
            go.transform.position = GetWorldPos(to);
        }

        /// <summary>
        /// スムーズ移動アニメーション。コルーチンを返す。
        /// BattleTurnManager で yield return して完了を待てる。
        /// </summary>
        public Coroutine MoveUnitSmooth(BattleUnit unit, HexCoord to)
        {
            if (_unitObjects == null || !_unitObjects.TryGetValue(unit.AgentId, out var go))
                return null;

            var from = go.transform.position;
            var dest = GetWorldPos(to);

            return StartCoroutine(AnimateMove(go.transform, from, dest, MoveDuration));
        }

        private IEnumerator AnimateMove(Transform t, Vector3 from, Vector3 dest, float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float ratio  = Mathf.Clamp01(elapsed / duration);
                // SmoothStep で加速→減速のイージング
                float smooth = ratio * ratio * (3f - 2f * ratio);
                t.position   = Vector3.Lerp(from, dest, smooth);
                yield return null;
            }
            t.position = dest;
        }

        public void RemoveUnit(BattleUnit unit)
        {
            if (_unitObjects == null || !_unitObjects.TryGetValue(unit.AgentId, out var go)) return;
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

            return team == BattleTeam.Player
                ? new Color32(0x60, 0xa0, 0xff, 0xff)
                : new Color32(0xff, 0x60, 0x60, 0xff);
        }

        private static Sprite CreateUnitSprite(Color32 body)
        {
            var tex    = new Texture2D(IconSize, IconSize) { filterMode = FilterMode.Point };
            var pixels = new Color32[IconSize * IconSize];
            var bg     = new Color32(0, 0, 0, 0);
            var dark   = new Color32(
                (byte)(body.r / 2), (byte)(body.g / 2), (byte)(body.b / 2), 0xff);

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
                new Vector2(0.5f, 0.5f), IconSize);
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
