using System;
using UnityEngine;

namespace DQSim
{
    public class TimeManager : MonoBehaviour
    {
        [Tooltip("1ゲーム日が何リアル秒か（デフォルト60秒 = 1実分で1日経過）")]
        public float realSecondsPerGameDay = 60f;

        public bool IsPaused { get; set; }

        public GameTime CurrentTime => new GameTime(_accumulatedGameSeconds);

        public event Action<GameTime> OnTimeChanged;

        private float _accumulatedGameSeconds;
        private float _gameSecondsPerRealSecond;

        private void Awake()
        {
            _gameSecondsPerRealSecond = 86400f / realSecondsPerGameDay;
        }

        private void Update()
        {
            if (IsPaused) return;

            _accumulatedGameSeconds += Time.deltaTime * _gameSecondsPerRealSecond;
            OnTimeChanged?.Invoke(CurrentTime);
        }

        public void Reset()
        {
            _accumulatedGameSeconds = 0f;
            IsPaused = false;
        }
    }
}
