// Assets/Scripts/Framework/Config/SettingsRegistry.cs
// AgentSim — JSON 設定の一元ロードと参照口
//
// 使い方:
//   GameBootstrap の最初の1行で呼ぶ:
//     SettingsRegistry.Load("adventurer_guild");
//
//   その後はどこからでも:
//     var cfg = SettingsRegistry.Current.Game;
//     var tiers = SettingsRegistry.Current.Tiers.tiers;

using System.IO;
using UnityEngine;
using Newtonsoft.Json;

namespace AgentSim.Config
{
    /// <summary>
    /// 全 JSON 設定を保持するシングルトン。
    /// GameBootstrap.Start() の先頭で SettingsRegistry.Load(settingId) を呼ぶこと。
    /// </summary>
    public class SettingsRegistry
    {
        // ── シングルトン ────────────────────────────────────────────────
        public static SettingsRegistry Current { get; private set; }

        // ── 設定プロパティ ──────────────────────────────────────────────
        public GameConfig             Game         { get; private set; }
        public StatDefinitions        Stats        { get; private set; }
        public TierConfig             Tiers        { get; private set; }
        public RoleConfig             Roles        { get; private set; }
        public OriginConfig           Origins      { get; private set; }
        public ContractTemplateConfig Contracts    { get; private set; }
        public ActionConfig           Actions      { get; private set; }
        public TileTypeConfig         TileTypes    { get; private set; }
        public MapConfig              MapSettings  { get; private set; }
        public BattleVisualConfig     BattleVisual { get; private set; }
        public OrgRankConfig          OrgRanks     { get; private set; }

        // ── ロード ──────────────────────────────────────────────────────
        /// <summary>
        /// StreamingAssets/settings/{settingId}/ 以下の全 JSON を読み込む。
        /// </summary>
        public static void Load(string settingId)
        {
            var r = new SettingsRegistry();
            string root = Path.Combine(Application.streamingAssetsPath, "settings", settingId);

            r.Game        = LoadJson<GameConfig>            (root, "game_config");
            r.Stats       = LoadJson<StatDefinitions>       (root, "stat_definitions");
            r.Tiers       = LoadJson<TierConfig>            (root, "tiers");
            r.Roles       = LoadJson<RoleConfig>            (root, "roles");
            r.Origins     = LoadJson<OriginConfig>          (root, "origins");
            r.Contracts   = LoadJson<ContractTemplateConfig>(root, "contract_templates");
            r.Actions     = LoadJson<ActionConfig>          (root, "actions");
            r.TileTypes   = LoadJson<TileTypeConfig>        (root, "tile_types");
            r.MapSettings = LoadJson<MapConfig>             (root, "map_config");

            // オプション: battle_visual.json が存在する場合のみ読み込む
            string bvPath = Path.Combine(root, "battle_visual.json");
            if (File.Exists(bvPath))
                r.BattleVisual = LoadJson<BattleVisualConfig>(root, "battle_visual");

            // オプション: org_ranks.json が存在する場合のみ読み込む
            string orPath = Path.Combine(root, "org_ranks.json");
            if (File.Exists(orPath))
                r.OrgRanks = LoadJson<OrgRankConfig>(root, "org_ranks");

            // 配列長の整合性チェック
            r.ValidateArrayLengths();

            Current = r;
            Debug.Log($"[SettingsRegistry] Loaded setting: '{settingId}'");
        }

        // ── ヘルパー ────────────────────────────────────────────────────
        private static T LoadJson<T>(string folder, string fileName)
        {
            string path = Path.Combine(folder, fileName + ".json");
            if (!File.Exists(path))
                throw new FileNotFoundException(
                    $"[SettingsRegistry] JSON file not found: {path}");

            string json = File.ReadAllText(path);
            return JsonConvert.DeserializeObject<T>(json);
        }

        /// <summary>
        /// stat_weights / stat_bonuses / stat_ranges の配列長が
        /// primary_stats の数と一致するか検証する。
        /// 不一致は設定ミスなので例外で早期に知らせる。
        /// </summary>
        private void ValidateArrayLengths()
        {
            int statCount = Stats.primary_stats.Length;

            foreach (var role in Roles.roles)
            {
                if (role.stat_weights != null && role.stat_weights.Length != statCount)
                    throw new System.InvalidOperationException(
                        $"[SettingsRegistry] roles.json: '{role.id}' の stat_weights 長({role.stat_weights.Length}) が " +
                        $"primary_stats 数({statCount}) と一致しません。");

                if (role.stat_ranges != null && role.stat_ranges.Length != statCount)
                    throw new System.InvalidOperationException(
                        $"[SettingsRegistry] roles.json: '{role.id}' の stat_ranges 長({role.stat_ranges.Length}) が " +
                        $"primary_stats 数({statCount}) と一致しません。");
            }

            foreach (var origin in Origins.origins)
            {
                if (origin.stat_bonuses != null && origin.stat_bonuses.Length != statCount)
                    throw new System.InvalidOperationException(
                        $"[SettingsRegistry] origins.json: '{origin.id}' の stat_bonuses 長({origin.stat_bonuses.Length}) が " +
                        $"primary_stats 数({statCount}) と一致しません。");
            }
        }

        // ── 便利アクセサー ──────────────────────────────────────────────
        /// <summary>id に一致する TierDef を返す（なければ null）</summary>
        public TierDef GetTier(string id)
        {
            foreach (var t in Tiers.tiers)
                if (t.id == id) return t;
            return null;
        }

        /// <summary>id に一致する RoleDef を返す（なければ null）</summary>
        public RoleDef GetRole(string id)
        {
            foreach (var r in Roles.roles)
                if (r.id == id) return r;
            return null;
        }

        /// <summary>id に一致する OriginDef を返す（なければ null）</summary>
        public OriginDef GetOrigin(string id)
        {
            foreach (var o in Origins.origins)
                if (o.id == id) return o;
            return null;
        }

        /// <summary>index に一致する TierDef を返す（なければ null）</summary>
        public TierDef GetTierByIndex(int index)
        {
            foreach (var t in Tiers.tiers)
                if (t.index == index) return t;
            return null;
        }

        /// <summary>
        /// ティアの人口分布に基づいて1つの TierDef をランダムに選ぶ。
        /// </summary>
        public TierDef PickRandomTier(System.Random rng)
        {
            float total = 0f;
            foreach (var t in Tiers.tiers) total += t.population_weight;

            float roll = (float)(rng.NextDouble() * total);
            float cum  = 0f;
            foreach (var t in Tiers.tiers)
            {
                cum += t.population_weight;
                if (roll <= cum) return t;
            }
            return Tiers.tiers[0];
        }

        /// <summary>
        /// totalEarned に対応する現在のランクを返す。
        /// OrgRanks が未定義なら null。
        /// </summary>
        public OrgRankDef GetCurrentOrgRank(int totalEarned)
        {
            var ranks = OrgRanks?.ranks;
            if (ranks == null || ranks.Length == 0) return null;
            OrgRankDef result = ranks[0];
            foreach (var r in ranks)
                if (totalEarned >= r.min_earned) result = r;
            return result;
        }

        /// <summary>id に一致する BattleTileDef を返す（なければ null）</summary>
        public BattleTileDef GetBattleTile(string id)
        {
            if (BattleVisual?.battle_tiles == null) return null;
            foreach (var t in BattleVisual.battle_tiles)
                if (t.id == id) return t;
            return null;
        }
    }
}

