using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DQSim
{
    public class GuildBoardPanel : MonoBehaviour
    {
        private RectTransform _content;
        private Button _rosterBtn;

        private GuildManager _guild;
        private DispatchPanel _dispatchPanel;
        private readonly List<GameObject> _questRows = new List<GameObject>();

        private void Awake()
        {
            var topBar = UIBuilder.TopBar(transform, 34, new Color(0.1f, 0.1f, 0.3f, 1f));
            UIBuilder.AddLabel(topBar.transform, "[ Guild Board ]", 15,
                new Color(0.9f, 0.8f, 0.3f), TextAlignmentOptions.Center);

            _content   = UIBuilder.ScrollContent(transform, topOffset: 34, bottomOffset: 46);
            _rosterBtn = UIBuilder.BottomBtn(transform, "View Roster", 14,
                new Color(0.25f, 0.4f, 0.75f), bottom: 4, height: 40);
        }

        public void Initialize(GuildManager guild, DispatchPanel dispatchPanel, RosterPanel rosterPanel)
        {
            _guild         = guild;
            _dispatchPanel = dispatchPanel;

            _guild.OnGuildStateChanged += RefreshQuests;
            _rosterBtn.onClick.AddListener(() => rosterPanel.Open());

            RefreshQuests();
        }

        private void RefreshQuests()
        {
            foreach (var r in _questRows) Destroy(r);
            _questRows.Clear();

            if (_guild.AvailableQuests.Count == 0)
            {
                var empty = MakeEmptyRow("No quests available");
                empty.transform.SetParent(_content, false);
                _questRows.Add(empty);
                return;
            }

            foreach (var quest in _guild.AvailableQuests)
            {
                var row = MakeQuestRow(quest);
                row.transform.SetParent(_content, false);
                _questRows.Add(row);
            }
        }

        private GameObject MakeQuestRow(Quest quest)
        {
            var go = UIBuilder.Row(_content, new Color(0.08f, 0.12f, 0.22f, 0.95f), 72f);

            UIBuilder.RowCell(go.transform,
                $"<b>{quest.Title}</b>\n<size=11>{quest.LocationName}  |  {AdventurerRankInfo.DisplayName(quest.MinRank)}+  |  {quest.RewardGold}G</size>",
                13f, Color.white,
                new Vector2(0f, 0f), new Vector2(0.72f, 1f));

            var btnGO = new GameObject("DispatchBtn");
            btnGO.transform.SetParent(go.transform, false);
            btnGO.AddComponent<Image>().color = new Color(0.2f, 0.5f, 0.9f, 1f);
            var btn   = btnGO.AddComponent<Button>();
            var btnRT = btnGO.GetComponent<RectTransform>();
            btnRT.anchorMin = new Vector2(0.74f, 0.15f);
            btnRT.anchorMax = new Vector2(0.98f, 0.85f);
            btnRT.offsetMin = Vector2.zero;
            btnRT.offsetMax = Vector2.zero;
            UIBuilder.AddLabel(btnGO.transform, "Dispatch", 13f, Color.white, TextAlignmentOptions.Center);

            var capturedQuest = quest;
            btn.onClick.AddListener(() => _dispatchPanel.Open(capturedQuest));

            return go;
        }

        private GameObject MakeEmptyRow(string text)
        {
            var go = UIBuilder.Row(_content, new Color(0f, 0f, 0f, 0.3f), 40f);
            UIBuilder.AddLabel(go.transform, text, 13f, new Color(0.6f, 0.6f, 0.6f), TextAlignmentOptions.Center);
            return go;
        }
    }
}
