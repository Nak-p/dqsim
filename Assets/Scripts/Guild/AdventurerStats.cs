using System;
using UnityEngine;

namespace DQSim
{
    public struct AdventurerStats
    {
        public int Strength;
        public int Vitality;
        public int Magic;
        public int Wisdom;
        public int Faith;

        public int HP             => 20 + Vitality * 5;
        public int MP             => 10 + Wisdom * 4;
        public int PhysicalAttack => 5 + Strength * 2;
        public int MagicAttack    => Magic * 2 + Wisdom / 2;
        public int HealPower      => Faith * 2 + Wisdom / 2;

        public int TotalPower =>
            HP / 5 + MP / 5 + PhysicalAttack + MagicAttack + HealPower;

        /// <summary>後方互換: ランダムランクで生成。</summary>
        public static AdventurerStats Generate(AdventurerJob job, AdventurerRace race, System.Random rng)
        {
            return GenerateForRank(job, race, AdventurerRankInfo.PickRank(rng), rng);
        }

        public static AdventurerStats GenerateForRank(
            AdventurerJob job,
            AdventurerRace race,
            AdventurerRank rank,
            System.Random rng)
        {
            var (mean, stddev) = AdventurerRankInfo.PowerProfile(rank);
            float targetPower = SampleGaussian(rng, mean, stddev);
            targetPower = Mathf.Max(18f, targetPower);

            var weights = AdventurerJobInfo.StatWeights(job);
            var bonus   = AdventurerRaceInfo.StatBonus(race);

            float wS = Mathf.Max(0.1f, weights[0] * (1f + bonus[0] * 0.1f));
            float wV = Mathf.Max(0.1f, weights[1] * (1f + bonus[1] * 0.1f));
            float wM = Mathf.Max(0.1f, weights[2] * (1f + bonus[2] * 0.1f));
            float wW = Mathf.Max(0.1f, weights[3] * (1f + bonus[3] * 0.1f));
            float wF = Mathf.Max(0.1f, weights[4] * (1f + bonus[4] * 0.1f));

            float denom = wV + 2f * wS + 2f * wM + 2f * wF + 1.8f * wW;
            if (denom < 0.01f)
                denom = 1f;

            int targetLinear = Mathf.RoundToInt(targetPower - 11f);
            targetLinear = Mathf.Max(targetLinear, 14);

            int v = Mathf.Max(1, Mathf.RoundToInt(targetLinear * wV / denom));
            int s = Mathf.Max(1, Mathf.RoundToInt(targetLinear * wS / denom));
            int m = Mathf.Max(1, Mathf.RoundToInt(targetLinear * wM / denom));
            int f = Mathf.Max(1, Mathf.RoundToInt(targetLinear * wF / denom));
            int w = Mathf.Max(1, Mathf.RoundToInt(targetLinear * wW / denom));

            var stats = new AdventurerStats
            {
                Vitality = v,
                Strength = s,
                Magic    = m,
                Wisdom   = w,
                Faith    = f,
            };

            NudgeTowardsTarget(ref stats, Mathf.RoundToInt(targetPower), job);
            return stats;
        }

        private static float SampleGaussian(System.Random rng, float mean, float stddev)
        {
            double u1 = 1.0 - rng.NextDouble();
            double u2 = rng.NextDouble();
            double z = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Cos(2.0 * Math.PI * u2);
            return mean + stddev * (float)z;
        }

        private static void NudgeTowardsTarget(ref AdventurerStats stats, int target, AdventurerJob job)
        {
            int[] priority = AdventurerJobInfo.StatPriority(job);

            for (int iter = 0; iter < 120; iter++)
            {
                int cur = stats.TotalPower;
                if (cur == target)
                    return;

                int dir = cur < target ? 1 : -1;

                int bestField = -1;
                int bestScore = int.MaxValue;

                for (int field = 0; field < 5; field++)
                {
                    var t = stats;
                    ApplyDelta(ref t, field, dir);
                    if (GetAt(ref t, field) < 1)
                        continue;

                    int score = Mathf.Abs(t.TotalPower - target);
                    bool better = bestField < 0 || score < bestScore;
                    if (!better && score == bestScore)
                    {
                        int idxNew  = PriorityIndex(priority, field);
                        int idxBest = PriorityIndex(priority, bestField);
                        better = dir > 0 ? idxNew < idxBest : idxNew > idxBest;
                    }

                    if (better)
                    {
                        bestScore = score;
                        bestField = field;
                    }
                }

                if (bestField < 0)
                    return;

                ApplyDelta(ref stats, bestField, dir);
            }
        }

        private static int PriorityIndex(int[] priority, int field)
        {
            for (int i = 0; i < priority.Length; i++)
            {
                if (priority[i] == field)
                    return i;
            }

            return 999;
        }

        private static int GetAt(ref AdventurerStats st, int field) => field switch
        {
            0 => st.Strength,
            1 => st.Vitality,
            2 => st.Magic,
            3 => st.Wisdom,
            4 => st.Faith,
            _ => 1
        };

        private static void ApplyDelta(ref AdventurerStats st, int field, int delta)
        {
            switch (field)
            {
                case 0: st.Strength += delta; break;
                case 1: st.Vitality += delta; break;
                case 2: st.Magic += delta; break;
                case 3: st.Wisdom += delta; break;
                case 4: st.Faith += delta; break;
            }
        }

        public override string ToString() =>
            $"STR:{Strength} VIT:{Vitality} MAG:{Magic} WIS:{Wisdom} FAI:{Faith}";
    }
}
