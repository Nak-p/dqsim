// Assets/Scripts/Framework/Config/ContractTemplateConfig.cs
// AgentSim — 案件（クエスト）テンプレート設定

namespace AgentSim.Config
{
    [System.Serializable]
    public class ContractTemplateConfig
    {
        // 案件タイトルのランダムプール
        public string[] title_pool;

        // 場所名のランダムプール
        public string[] location_pool;

        // 説明文テンプレート。{location} トークンが場所名に置換される
        // 例: "Contract at {location}. Fulfill the requirements and secure the area."
        public string   description_template;

        // パーティ人数制約
        public int      min_party_size;
        public int      max_party_size;

        // ベース地点からの最低距離（タイル数）
        public int      min_quest_distance;
    }
}
