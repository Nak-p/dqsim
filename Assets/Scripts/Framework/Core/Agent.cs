// Assets/Scripts/Framework/Core/Agent.cs
// AgentSim — 派遣可能なエージェント（冒険者 / ロボット / 傭兵 など）
//
// 世界観の名称（職業名・ランク名など）は C# に書かず、
// roleId / originId / tierId の id 文字列を通じて JSON から取得する。

using System;
using AgentSim.Config;

namespace AgentSim.Core
{
    public class Agent
    {
        // ── 識別 ──────────────────────────────────────────────────────
        public string Id { get; } = Guid.NewGuid().ToString();

        // ── 表示情報 ──────────────────────────────────────────────────
        public string Name;
        public int    Age;

        // ── 分類（id 文字列で参照 → JSON から表示名を取得） ──────────
        public string RoleId;    // "warrior", "heavy", "recon" など
        public string OriginId;  // "human", "elf", "standard_frame" など
        public string TierId;    // "copper", "iron", "mark_i" など

        // ── ステータス ────────────────────────────────────────────────
        public AgentStats Stats;

        // ── 状態 ──────────────────────────────────────────────────────
        public bool IsAvailable = true;

        // ── 通貨（名称は SettingsRegistry.Current.Game.currency_name を使う） ──
        public int EarnedCurrency;   // 累計獲得通貨
        public int CurrentCurrency;  // 現在の所持通貨

        // ── JSON 定義へのアクセサー ───────────────────────────────────
        public RoleDef   Role   => SettingsRegistry.Current.GetRole(RoleId);
        public OriginDef Origin => SettingsRegistry.Current.GetOrigin(OriginId);
        public TierDef   Tier   => SettingsRegistry.Current.GetTier(TierId);

        // ── 表示テキスト ──────────────────────────────────────────────
        public string StatusText => IsAvailable ? "Available" : "On Contract";

        public string ShortLabel =>
            $"{Name}  [{Role?.display_name ?? RoleId}" +
            $"|{Origin?.display_name ?? OriginId}" +
            $"|{Tier?.display_name ?? TierId}]";

        // ── 生成ファクトリ ────────────────────────────────────────────
        /// <summary>
        /// SettingsRegistry の設定に基づいてランダムなエージェントを生成する。
        /// </summary>
        public static Agent Generate(System.Random rng)
        {
            var reg = SettingsRegistry.Current;

            // ロールをランダム選択
            var roles = reg.Roles.roles;
            var role  = roles[rng.Next(roles.Length)];

            // オリジンをランダム選択
            var origins = reg.Origins.origins;
            var origin  = origins[rng.Next(origins.Length)];

            // ティアを人口分布に基づいてランダム選択
            var tier = reg.PickRandomTier(rng);

            // 名前をオリジンのプールから選択
            string name = origin.name_pool[rng.Next(origin.name_pool.Length)];

            // ステータス生成
            var stats = AgentStats.Generate(role, origin, tier, rng);

            return new Agent
            {
                Name           = name,
                Age            = rng.Next(18, 56),
                RoleId         = role.id,
                OriginId       = origin.id,
                TierId         = tier.id,
                Stats          = stats,
                IsAvailable    = true,
                EarnedCurrency = 0,
                CurrentCurrency = 0,
            };
        }
    }
}
