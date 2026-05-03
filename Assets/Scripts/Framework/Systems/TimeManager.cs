// Assets/Scripts/Framework/Systems/TimeManager.cs
// AgentSim — ゲーム内時間の進行管理 MonoBehaviour
//
// GameBootstrap が保持し、実時間を GameTime に変換して配信する。
// パラメータ realSecondsPerGameDay は JSON から読み込む（ハードコーディング禁止）。

using System;
using UnityEngine;
using AgentSim.Core;

namespace AgentSim.Systems
{
    public class TimeManager : MonoBehaviour
    {
        // ── パラメータ（Inspector / GameBootstrap が設定） ────────────
        // ゲーム内 1日 = 現実の何秒か（デフォルト 60 秒）
        // 注: この値は Inspector からテスト用に調整可能だが、
        //     将来的には game_config.json に移す
        [SerializeField] private float realSecondsPerGameDay = 60f;

        // ── 状態 ──────────────────────────────────────────────────────
        private float _accumulatedSeconds;
        public  bool  IsPaused { get; private set; }

        // ── プロパティ ────────────────────────────────────────────────
        public GameTime CurrentTime => new GameTime(_accumulatedSeconds);

        /// <summary>ゲーム内 1日が現実の何秒に相当するか（PartyController などで参照）</summary>
        public float RealSecondsPerGameDay => realSecondsPerGameDay;

        // ── イベント ──────────────────────────────────────────────────
        public event Action<GameTime> OnTimeChanged;

        // ── Unity ライフサイクル ──────────────────────────────────────
        private void Update()
        {
            if (IsPaused) return;

            float gameSecondsPerRealSecond = 86400f / realSecondsPerGameDay;
            _accumulatedSeconds += Time.deltaTime * gameSecondsPerRealSecond;
            OnTimeChanged?.Invoke(CurrentTime);
        }

        // ── 公開 API ─────────────────────────────────────────────────
        public void Pause()  => IsPaused = true;
        public void Resume() => IsPaused = false;

        public void Reset()
        {
            _accumulatedSeconds = 0f;
            OnTimeChanged?.Invoke(CurrentTime);
        }

        /// <summary>現在時刻から deltaHours 後の GameTime を返す</summary>
        public GameTime GetFutureTime(float deltaHours)
            => new GameTime(_accumulatedSeconds + deltaHours * 3600f);
    }
}
