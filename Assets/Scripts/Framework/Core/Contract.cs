// Assets/Scripts/Framework/Core/Contract.cs
// AgentSim — 派遣案件（クエスト / ジョブ など）
//
// 案件の固有名詞（タイトル・場所名）は contract_templates.json から生成する。
// 報酬通貨の名称は GameConfig.currency_name を使う。

using System;
using UnityEngine;
using AgentSim.Config;

namespace AgentSim.Core
{
    public class Contract
    {
        // ── 識別 ──────────────────────────────────────────────────────
        public string Id { get; } = Guid.NewGuid().ToString();

        // ── 表示情報 ──────────────────────────────────────────────────
        public string Title;
        public string Description;
        public string LocationName;

        // ── マップ上の目的地 ──────────────────────────────────────────
        public Vector2Int Destination;

        // ── 条件 ──────────────────────────────────────────────────────
        public string MinTierId;       // このティア以上のエージェントが必要
        public int    MinPartySize;
        public int    MaxPartySize;

        // ── 報酬（通貨名は GameConfig.currency_name） ─────────────────
        public int Reward;

        // ── アクセサー ────────────────────────────────────────────────
        public TierDef MinTier => SettingsRegistry.Current.GetTier(MinTierId);

        // ── 生成ファクトリ ────────────────────────────────────────────
        /// <summary>
        /// contract_templates.json と tiers.json をもとにランダムな案件を生成する。
        /// </summary>
        /// <param name="destination">マップ上のランダムなタイル座標</param>
        /// <param name="tierIndex">要求ティアの index（0 〜 tiers.Length-1）</param>
        public static Contract Generate(Vector2Int destination, int tierIndex, System.Random rng)
        {
            var reg  = SettingsRegistry.Current;
            var tmpl = reg.Contracts;
            var tier = reg.GetTierByIndex(tierIndex) ?? reg.Tiers.tiers[0];

            string title    = tmpl.title_pool[rng.Next(tmpl.title_pool.Length)];
            string location = tmpl.location_pool[rng.Next(tmpl.location_pool.Length)];
            string desc     = tmpl.description_template.Replace("{location}", location);

            int reward = rng.Next(tier.contract_reward_min, tier.contract_reward_max + 1);

            return new Contract
            {
                Title        = title,
                Description  = desc,
                LocationName = location,
                Destination  = destination,
                MinTierId    = tier.id,
                MinPartySize = tmpl.min_party_size,
                MaxPartySize = tmpl.max_party_size,
                Reward       = reward,
            };
        }
    }
}
