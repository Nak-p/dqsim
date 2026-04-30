using System;

namespace DQSim
{
    public struct GameTime
    {
        public float TotalSeconds;

        public int Day => (int)(TotalSeconds / 86400f) + 1;
        public int Hour => (int)(TotalSeconds % 86400f / 3600f);
        public int Minute => (int)(TotalSeconds % 3600f / 60f);

        public GameTime(float totalSeconds)
        {
            TotalSeconds = totalSeconds;
        }

        public static GameTime operator +(GameTime a, float addSeconds) =>
            new GameTime(a.TotalSeconds + addSeconds);

        public static float operator -(GameTime a, GameTime b) =>
            a.TotalSeconds - b.TotalSeconds;

        public override string ToString() =>
            $"Day {Day}  {Hour:D2}:{Minute:D2}";

        public string ToETAString(GameTime current)
        {
            float remaining = Math.Max(0f, TotalSeconds - current.TotalSeconds);
            int days = (int)(remaining / 86400f);
            int hours = (int)(remaining % 86400f / 3600f);
            int minutes = (int)(remaining % 3600f / 60f);

            if (days > 0)
                return $"{days}d {hours}h {minutes}m";
            if (hours > 0)
                return $"{hours}h {minutes}m";
            return $"{minutes}m";
        }
    }
}
