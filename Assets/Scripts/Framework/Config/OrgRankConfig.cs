// Assets/Scripts/Framework/Config/OrgRankConfig.cs
// AgentSim — 企業ランク定義の JSON マッピング
// org_ranks.json を C# にマップする。

namespace AgentSim.Config
{
    public class OrgRankConfig
    {
        public OrgRankDef[] ranks;
    }

    public class OrgRankDef
    {
        /// <summary>小文字スネークケースの識別子</summary>
        public string id;

        /// <summary>UI 表示名（SettingsRegistry 経由で取得する）</summary>
        public string display_name;

        /// <summary>このランクに達するための TotalOrgEarned の最低値</summary>
        public int min_earned;

        /// <summary>企業規模を表す数値（1 が最小）</summary>
        public int scale;
    }
}
