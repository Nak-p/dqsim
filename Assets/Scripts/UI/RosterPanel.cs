using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DQSim
{
    public class RosterPanel : MonoBehaviour
    {
        private RectTransform _content;

        private GuildManager _guild;

        private void Awake()
        {
            var topBar = UIBuilder.TopBar(transform, 34, new Color(0.1f, 0.1f, 0.3f, 1f));
            UIBuilder.AddLabel(topBar.transform, "Guild Adventurers", 15,
                new Color(0.9f, 0.8f, 0.3f), TextAlignmentOptions.Center);

            _content = UIBuilder.ScrollContent(transform, topOffset: 34, bottomOffset: 52);

            var closeBtn = UIBuilder.BottomBtn(transform, "Close", 15,
                new Color(0.3f, 0.3f, 0.5f, 1f), bottom: 4, height: 44);
            closeBtn.onClick.AddListener(() => gameObject.SetActive(false));
        }

        public void Initialize(GuildManager guild)
        {
            _guild = guild;
            _guild.OnGuildStateChanged += () => { if (gameObject.activeSelf) Refresh(); };
            gameObject.SetActive(false);
        }

        public void Open()
        {
            Refresh();
            gameObject.SetActive(true);
        }

        private void Refresh()
        {
            foreach (Transform child in _content) Destroy(child.gameObject);
            AddHeaderRow();
            foreach (var adv in _guild.Adventurers)
                AddAdventurerRow(adv);
        }

        private void AddHeaderRow()
        {
            var go = UIBuilder.Row(_content, new Color(0.2f, 0.2f, 0.35f, 1f), 28f);
            go.transform.SetParent(_content, false);
            UIBuilder.RowCell(go.transform, "Name",   13f, Color.yellow, new Vector2(0f,    0f), new Vector2(0.20f, 1f));
            UIBuilder.RowCell(go.transform, "Job",    13f, Color.yellow, new Vector2(0.20f, 0f), new Vector2(0.34f, 1f));
            UIBuilder.RowCell(go.transform, "Race",   13f, Color.yellow, new Vector2(0.34f, 0f), new Vector2(0.46f, 1f));
            UIBuilder.RowCell(go.transform, "Rank",   13f, Color.yellow, new Vector2(0.46f, 0f), new Vector2(0.56f, 1f));
            UIBuilder.RowCell(go.transform, "Status", 13f, Color.yellow, new Vector2(0.56f, 0f), new Vector2(0.70f, 1f));
            UIBuilder.RowCell(go.transform, "Gold",   12f, Color.yellow, new Vector2(0.70f, 0f), new Vector2(0.82f, 1f));
            UIBuilder.RowCell(go.transform, "Stats",  11f, Color.yellow, new Vector2(0.82f, 0f), new Vector2(1f,    1f));
        }

        private void AddAdventurerRow(Adventurer adv)
        {
            var bgColor = adv.IsAvailable
                ? new Color(0.1f, 0.15f, 0.25f, 1f)
                : new Color(0.15f, 0.1f, 0.1f, 1f);
            var go = UIBuilder.Row(_content, bgColor, 38f);
            go.transform.SetParent(_content, false);

            Color rankColor   = AdventurerRankInfo.BadgeColor(adv.Rank);
            Color statusColor = adv.IsAvailable
                ? new Color(0.4f, 0.9f, 0.4f)
                : new Color(0.9f, 0.5f, 0.3f);

            UIBuilder.RowCell(go.transform, adv.Name,                                                        13f, Color.white,  new Vector2(0f,    0f), new Vector2(0.20f, 1f));
            UIBuilder.RowCell(go.transform, AdventurerJobInfo.DisplayName(adv.Job),                           12f, Color.white,  new Vector2(0.20f, 0f), new Vector2(0.34f, 1f));
            UIBuilder.RowCell(go.transform, AdventurerRaceInfo.DisplayName(adv.Race),                         12f, Color.white,  new Vector2(0.34f, 0f), new Vector2(0.46f, 1f));
            UIBuilder.RowCell(go.transform, AdventurerRankInfo.DisplayName(adv.Rank),                         12f, rankColor,    new Vector2(0.46f, 0f), new Vector2(0.56f, 1f));
            UIBuilder.RowCell(go.transform, adv.StatusText,                                                   12f, statusColor,  new Vector2(0.56f, 0f), new Vector2(0.70f, 1f));
            UIBuilder.RowCell(go.transform, $"{adv.EarnedGold}G",                                             11f, new Color(1f, 0.85f, 0.35f), new Vector2(0.70f, 0f), new Vector2(0.82f, 1f));
            UIBuilder.RowCell(go.transform, $"S{adv.Stats.Strength} V{adv.Stats.Vitality} M{adv.Stats.Magic}", 11f, Color.white, new Vector2(0.82f, 0f), new Vector2(1f,    1f));
        }
    }
}
