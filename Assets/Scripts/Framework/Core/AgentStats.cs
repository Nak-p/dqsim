// Assets/Scripts/Framework/Core/AgentStats.cs
// AgentSim — エージェントのステータスコンテナ
//
// ステータス名・導出式はすべて JSON(stat_definitions.json) から読み込む。
// C# にステータス名・数値をハードコーディングしてはいけない。

using System;
using AgentSim.Config;

namespace AgentSim.Core
{
    public class AgentStats
    {
        // ── フィールド ────────────────────────────────────────────────
        // primary_stats の index に対応する整数配列
        // StatDefinitions.primary_stats と同じ順序
        private readonly int[] _primary;

        // ── コンストラクタ ────────────────────────────────────────────
        public AgentStats(int[] primaryValues)
        {
            _primary = primaryValues;
        }

        // ── アクセサー ────────────────────────────────────────────────
        /// <summary>primary stat を index で取得</summary>
        public int GetPrimary(int index) => _primary[index];

        /// <summary>primary stat を id 文字列で取得</summary>
        public int GetPrimaryById(string statId)
        {
            var defs = SettingsRegistry.Current.Stats;
            for (int i = 0; i < defs.primary_stats.Length; i++)
                if (defs.primary_stats[i].id == statId) return _primary[i];
            throw new ArgumentException($"[AgentStats] Unknown primary stat id: '{statId}'");
        }

        /// <summary>derived stat を id 文字列で取得（formula を FormulaEvaluator で評価）</summary>
        public int GetDerived(string statId)
        {
            var defs = SettingsRegistry.Current.Stats;
            foreach (var d in defs.derived_stats)
            {
                if (d.id == statId)
                    return (int)FormulaEvaluator.Evaluate(d.formula, this, defs);
            }
            throw new ArgumentException($"[AgentStats] Unknown derived stat id: '{statId}'");
        }

        /// <summary>総合戦力（total_power_formula を評価）</summary>
        public int TotalPower =>
            (int)FormulaEvaluator.Evaluate(
                SettingsRegistry.Current.Stats.total_power_formula,
                this,
                SettingsRegistry.Current.Stats);

        // ── 生成 ──────────────────────────────────────────────────────
        /// <summary>
        /// ロール・オリジン・ティアに基づいてランダムなステータスを生成する。
        /// </summary>
        public static AgentStats Generate(RoleDef role, OriginDef origin, TierDef tier, Random rng)
        {
            var defs = SettingsRegistry.Current.Stats;
            int statCount = defs.primary_stats.Length;

            // ① ティアの戦力目標をガウス分布でサンプリング
            float targetPower = SampleGaussian(rng, tier.power_mean, tier.power_stddev);

            // ② ロールの重みで戦力を各ステータスに配分
            float weightSum = 0f;
            for (int i = 0; i < statCount; i++) weightSum += role.stat_weights[i];

            var primary = new int[statCount];
            for (int i = 0; i < statCount; i++)
            {
                float share = targetPower * (role.stat_weights[i] / weightSum);
                primary[i] = (int)Math.Max(share, 1f);
            }

            // ③ オリジンのボーナスを加算
            for (int i = 0; i < statCount; i++)
                primary[i] = Math.Max(1, primary[i] + origin.stat_bonuses[i]);

            // ④ stat_ranges でクランプ
            for (int i = 0; i < statCount; i++)
            {
                int lo = role.stat_ranges[i][0];
                int hi = role.stat_ranges[i][1];
                primary[i] = Math.Max(lo, Math.Min(hi, primary[i]));
            }

            return new AgentStats(primary);
        }

        // ── 内部ユーティリティ ────────────────────────────────────────
        private static float SampleGaussian(Random rng, float mean, float stddev)
        {
            // Box-Muller 変換
            double u1 = 1.0 - rng.NextDouble();
            double u2 = 1.0 - rng.NextDouble();
            double z  = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Cos(2.0 * Math.PI * u2);
            return mean + stddev * (float)z;
        }
    }
}
