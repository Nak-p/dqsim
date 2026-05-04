// Assets/Scripts/Framework/Party/PartyController.cs
// AgentSim — マップ上のパーティ移動アニメーション MonoBehaviour
//
// 移動速度は SettingsRegistry.Current.Game.character_travel_speed を動的参照する。
// パーティの色は呼び出し元が Color32 で渡す（世界観非依存）。

using System;
using System.Collections.Generic;
using UnityEngine;
using AgentSim.Config;
using AgentSim.Systems;

namespace AgentSim.Party
{
    public class PartyController : MonoBehaviour
    {
        // ── イベント ──────────────────────────────────────────────────
        public event Action OnArrivedAtDestination;
        public event Action OnArrivedAtBase;

        // ── 内部状態 ──────────────────────────────────────────────────
        private TimeManager      _timeManager;
        private List<Vector2Int> _path;
        private int   _pathIndex;
        private float _progress;
        private bool  _isMoving;
        private bool  _goingToDestination;
        private SpriteRenderer _spriteRenderer;

        // ── Unity ライフサイクル ──────────────────────────────────────
        private void Awake()
        {
            _spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
            _spriteRenderer.sprite       = CreatePartySprite(new Color32(0xf0, 0xd0, 0x60, 0xff));
            _spriteRenderer.sortingOrder = 10;
        }

        // ── 初期化 ────────────────────────────────────────────────────
        /// <summary>
        /// TimeManager を渡して初期化する。
        /// partyColor.a > 0 の場合はスプライト色を上書きする。
        /// </summary>
        public void Initialize(TimeManager timeManager, Color32 partyColor = default)
        {
            _timeManager = timeManager;
            if (partyColor.a > 0)
                _spriteRenderer.sprite = CreatePartySprite(partyColor);
        }

        // ── 移動制御 ──────────────────────────────────────────────────
        /// <summary>path に沿って移動を開始する。toDestination=true なら目的地方向。</summary>
        public void StartMoving(List<Vector2Int> path, bool toDestination)
        {
            if (path == null || path.Count < 2) return;
            _path               = path;
            _pathIndex          = 1;
            _progress           = 0f;
            _isMoving           = true;
            _goingToDestination = toDestination;
            transform.position  = TileToWorld(path[0]);
        }

        /// <summary>指定タイルに即座に配置する（テレポート）。</summary>
        public void PlaceAt(Vector2Int tile)
        {
            transform.position = TileToWorld(tile);
            _isMoving = false;
            _path     = null;
        }

        // ── Update ────────────────────────────────────────────────────
        private void Update()
        {
            if (!_isMoving || _timeManager == null || _timeManager.IsPaused) return;

            // 移動速度を JSON 設定から動的参照（character_travel_speed: タイル/ゲーム内時間）
            float partySpeed = SettingsRegistry.Current?.Game?.character_travel_speed ?? 1f;

            float gameSecondsPerRealSecond = 86400f / _timeManager.RealSecondsPerGameDay;
            float inGameHoursThisFrame     = Time.deltaTime * gameSecondsPerRealSecond / 3600f;
            float tilesThisFrame           = partySpeed * inGameHoursThisFrame;

            _progress += tilesThisFrame;

            // 進行度が 1.0 を超えたら次のノードへ進む
            while (_progress >= 1f && _pathIndex < _path.Count - 1)
            {
                _progress -= 1f;
                _pathIndex++;
            }

            // 終点に到達
            if (_pathIndex >= _path.Count - 1 && _progress >= 1f)
            {
                transform.position = TileToWorld(_path[_path.Count - 1]);
                _isMoving = false;
                _progress = 0f;

                if (_goingToDestination) OnArrivedAtDestination?.Invoke();
                else                     OnArrivedAtBase?.Invoke();
                return;
            }

            // ノード間を線形補間
            transform.position = Vector3.Lerp(
                TileToWorld(_path[_pathIndex - 1]),
                TileToWorld(_path[_pathIndex]),
                _progress);
        }

        // ── 内部ユーティリティ ────────────────────────────────────────
        private static Vector3 TileToWorld(Vector2Int tile) =>
            new Vector3(tile.x + 0.5f, tile.y + 0.5f, -1f);

        /// <summary>
        /// 指定色のパーティアイコンスプライトを手続き生成する。
        /// テクスチャサイズ・形状はアルゴリズム定数（ゲームパラメータではない）。
        /// </summary>
        private static Sprite CreatePartySprite(Color32 body)
        {
            const int Size = 12;
            var tex    = new Texture2D(Size, Size) { filterMode = FilterMode.Point };
            var pixels = new Color32[Size * Size];
            Color32 bg   = new Color32(0, 0, 0, 0);
            Color32 dark = new Color32((byte)(body.r / 2), (byte)(body.g / 2), (byte)(body.b / 2), 0xff);

            for (int i = 0; i < pixels.Length; i++) pixels[i] = bg;

            // 胴体・腕・頭（2人分）
            SetRect(pixels, Size, 4,  9, 7, 11, body);   // 脚
            SetRect(pixels, Size, 3,  5, 8,  8, body);   // 胴
            SetRect(pixels, Size, 1,  5, 2,  7, body);   // 左腕
            SetRect(pixels, Size, 9,  5, 10, 7, body);   // 右腕
            SetRect(pixels, Size, 3,  2, 5,  4, body);   // 頭1
            SetRect(pixels, Size, 6,  2, 8,  4, body);   // 頭2
            SetRect(pixels, Size, 4,  9, 7,  9, dark);   // 影

            tex.SetPixels32(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, Size, Size), new Vector2(0.5f, 0.5f), Size);
        }

        private static void SetRect(Color32[] pixels, int size, int x0, int y0, int x1, int y1, Color32 color)
        {
            for (int y = y0; y <= y1; y++)
                for (int x = x0; x <= x1; x++)
                    if (x >= 0 && x < size && y >= 0 && y < size)
                        pixels[y * size + x] = color;
        }
    }
}
