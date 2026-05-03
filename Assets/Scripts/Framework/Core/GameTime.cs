// Assets/Scripts/Framework/Core/GameTime.cs
// AgentSim — ゲーム内時刻の構造体
//
// 実時間とは独立したゲーム内時間。
// TimeManager が TotalSeconds を積算し、Day/Hour/Minute を導出する。

using System;

namespace AgentSim.Core
{
    [Serializable]
    public struct GameTime
    {
        // ── データ ──────────────────────────────────────────���────────
        public float TotalSeconds;

        // ── 導出プロパティ ────────────────────────────────────────────
        public int Day    => (int)(TotalSeconds / 86400f) + 1;
        public int Hour   => (int)(TotalSeconds % 86400f / 3600f);
        public int Minute => (int)(TotalSeconds % 3600f  / 60f);

        // ── コンストラクタ ────────────────────────────────────────────
        public GameTime(float totalSeconds) { TotalSeconds = totalSeconds; }

        // ── 演算子 ────────────────────────────────────────────────────
        public static GameTime operator +(GameTime a, GameTime b)
            => new GameTime(a.TotalSeconds + b.TotalSeconds);

        public static GameTime operator -(GameTime a, GameTime b)
            => new GameTime(a.TotalSeconds - b.TotalSeconds);

        public static bool operator <(GameTime a, GameTime b)
            => a.TotalSeconds < b.TotalSeconds;

        public static bool operator >(GameTime a, GameTime b)
            => a.TotalSeconds > b.TotalSeconds;

        public static bool operator <=(GameTime a, GameTime b)
            => a.TotalSeconds <= b.TotalSeconds;

        public static bool operator >=(GameTime a, GameTime b)
            => a.TotalSeconds >= b.TotalSeconds;

        // ── 表示 ──────────────────────────────────────────────────────
        public override string ToString()
            => $"Day {Day}  {Hour:D2}:{Minute:D2}";

        /// <summary>現在時刻から見た残り時間を "3d 4h 12m" 形式で返す</summary>
        public string ToETAString(GameTime current)
        {
            float remaining = TotalSeconds - current.TotalSeconds;
            if (remaining <= 0f) return "Arrived";

            int d = (int)(remaining / 86400f);
            int h = (int)(remaining % 86400f / 3600f);
            int m = (int)(remaining % 3600f  / 60f);

            if (d > 0)  return $"{d}d {h}h {m}m";
            if (h > 0)  return $"{h}h {m}m";
            return $"{m}m";
        }

        /// <summary>ゲーム時間（時間単位）から GameTime を生成するファクトリ</summary>
        public static GameTime FromHours(float hours)
            => new GameTime(hours * 3600f);
    }
}
