using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DQSim
{
    /// <summary>
    /// Full-screen Guild Hall overlay.
    /// 3-column layout: Quest Board | Active Missions | Roster.
    /// Activated by the Guild Hall button on the map screen.
    /// </summary>
    public class GuildScreen : MonoBehaviour
    {
        // Column content transforms
        private RectTransform _questContent;
        private RectTransform _missionContent;
        private RectTransform _rosterContent;

        // Top-bar gold display
        private TextMeshProUGUI _goldLabel;

        // Cached row lists for cleanup
        private readonly List<GameObject> _questRows   = new List<GameObject>();
        private readonly List<GameObject> _missionRows = new List<GameObject>();

        // Manager references (set via Initialize)
        private GuildManager   _guild;
        private MissionManager _missionMgr;
        private TimeManager    _timeMgr;
        private DispatchPanel  _dispatchPanel;

        // ─── Lifecycle ────────────────────────────────────────────────────

        private void Awake()
        {
            // Full-screen dark background (Image added by SceneSetup)
            var bg = GetComponent<Image>();
            if (bg == null) bg = gameObject.AddComponent<Image>();
            bg.color = new Color(0.02f, 0.03f, 0.12f, 0.97f);

            // ── Top bar ──────────────────────────────────────────────────
            var topBar = UIBuilder.TopBar(transform, 44, new Color(0.05f, 0.05f, 0.25f, 1f));

            // Gold label – left side
            var goldGO       = new GameObject("GoldLabel");
            goldGO.transform.SetParent(topBar.transform, false);
            _goldLabel           = goldGO.AddComponent<TextMeshProUGUI>();
            _goldLabel.fontSize  = 14f;
            _goldLabel.color     = new Color(1f, 0.85f, 0.3f);
            _goldLabel.alignment = TextAlignmentOptions.MidlineLeft;
            var goldRT   = goldGO.GetComponent<RectTransform>();
            goldRT.anchorMin = new Vector2(0f,   0.1f);
            goldRT.anchorMax = new Vector2(0.22f, 0.9f);
            goldRT.offsetMin = new Vector2(12, 0);
            goldRT.offsetMax = Vector2.zero;

            // Title – center
            UIBuilder.AddLabel(topBar.transform, "GUILD HALL", 20,
                new Color(1f, 0.85f, 0.2f), TextAlignmentOptions.Center);

            // Return button – right side
            var retGO = new GameObject("ReturnBtn");
            retGO.transform.SetParent(topBar.transform, false);
            retGO.AddComponent<Image>().color = new Color(0.4f, 0.1f, 0.1f, 1f);
            var retBtn = retGO.AddComponent<Button>();
            var retRT  = retGO.GetComponent<RectTransform>();
            retRT.anchorMin        = new Vector2(1f, 0.1f);
            retRT.anchorMax        = new Vector2(1f, 0.9f);
            retRT.pivot            = new Vector2(1f, 0.5f);
            retRT.anchoredPosition = new Vector2(-6f, 0f);
            retRT.sizeDelta        = new Vector2(180f, 0f);
            UIBuilder.AddLabel(retGO.transform, "Return to Map", 13,
                Color.white, TextAlignmentOptions.Center);
            retBtn.onClick.AddListener(Close);

            // ── 3 columns (start below top bar, fill rest of screen) ─────
            _questContent   = MakeColumn("QuestBoard",  0f,     0.41f,  "[ Quest Board ]");
            _missionContent = MakeColumn("Missions",    0.425f, 0.625f, "[ Active Missions ]");
            _rosterContent  = MakeColumn("Roster",      0.64f,  1f,     "[ Roster ]");

            gameObject.SetActive(false);
        }

        // Creates a column child with its own title bar + scroll content.
        private RectTransform MakeColumn(string colName, float xMin, float xMax, string title)
        {
            var col = new GameObject(colName);
            col.transform.SetParent(transform, false);
            col.AddComponent<Image>().color = new Color(0.04f, 0.04f, 0.16f, 0.6f);
            var rt       = col.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(xMin, 0f);
            rt.anchorMax = new Vector2(xMax, 1f);
            rt.pivot     = new Vector2(0.5f, 0.5f);
            rt.offsetMin = new Vector2(4f, 4f);
            rt.offsetMax = new Vector2(-4f, -48f);   // 48 = top bar height + 4 gap

            var bar = UIBuilder.TopBar(col.transform, 32, new Color(0.1f, 0.1f, 0.32f, 1f));
            UIBuilder.AddLabel(bar.transform, title, 13,
                new Color(0.9f, 0.8f, 0.3f), TextAlignmentOptions.Center);

            return UIBuilder.ScrollContent(col.transform, topOffset: 32, bottomOffset: 4);
        }

        // ─── Public API ───────────────────────────────────────────────────

        public void Initialize(GuildManager guild, MissionManager missionMgr,
            TimeManager timeMgr, DispatchPanel dispatchPanel)
        {
            _guild         = guild;
            _missionMgr    = missionMgr;
            _timeMgr       = timeMgr;
            _dispatchPanel = dispatchPanel;

            // Subscribe to events — each handler guards against inactive screen
            _guild.OnGuildStateChanged += () =>
            {
                if (!gameObject.activeSelf) return;
                RefreshQuests();
                RefreshRoster();
                UpdateGold();
            };
            _missionMgr.OnArrivalAtDestination += _ =>
            {
                if (gameObject.activeSelf) RefreshMissions();
            };
            _missionMgr.OnReturnedToBase += _ =>
            {
                if (!gameObject.activeSelf) return;
                RefreshMissions();
                RefreshRoster();
                UpdateGold();
            };
            // Time tick: only refresh mission ETAs while screen is open
            _timeMgr.OnTimeChanged += _ =>
            {
                if (gameObject.activeSelf) RefreshMissions();
            };
        }

        public void Open()
        {
            gameObject.SetActive(true);
            UpdateGold();
            RefreshQuests();
            RefreshMissions();
            RefreshRoster();
        }

        public void Close() => gameObject.SetActive(false);

        // ─── Internal refresh ─────────────────────────────────────────────

        private void UpdateGold() => _goldLabel.text = $"Gold: {_guild.GuildGold}G";

        // ── Quest Board ───────────────────────────────────────────────────

        private void RefreshQuests()
        {
            foreach (var r in _questRows) Destroy(r);
            _questRows.Clear();

            if (_guild.AvailableQuests.Count == 0)
            {
                _questRows.Add(MakeEmptyRow(_questContent, "No quests available"));
                return;
            }
            foreach (var quest in _guild.AvailableQuests)
                _questRows.Add(MakeQuestRow(quest));
        }

        private GameObject MakeQuestRow(Quest quest)
        {
            var go = UIBuilder.Row(_questContent, new Color(0.08f, 0.12f, 0.22f, 0.95f), 72f);

            UIBuilder.RowCell(go.transform,
                $"<b>{quest.Title}</b>\n" +
                $"<size=11>{quest.LocationName}  |  " +
                $"{AdventurerRankInfo.DisplayName(quest.MinRank)}+  |  {quest.RewardGold}G</size>",
                13f, Color.white, new Vector2(0f, 0f), new Vector2(0.72f, 1f));

            // Dispatch button
            var btnGO = new GameObject("DispatchBtn");
            btnGO.transform.SetParent(go.transform, false);
            btnGO.AddComponent<Image>().color = new Color(0.2f, 0.5f, 0.9f, 1f);
            var btn   = btnGO.AddComponent<Button>();
            var btnRT = btnGO.GetComponent<RectTransform>();
            btnRT.anchorMin = new Vector2(0.74f, 0.15f);
            btnRT.anchorMax = new Vector2(0.98f, 0.85f);
            btnRT.offsetMin = Vector2.zero;
            btnRT.offsetMax = Vector2.zero;
            UIBuilder.AddLabel(btnGO.transform, "Dispatch", 13f,
                Color.white, TextAlignmentOptions.Center);

            var capturedQuest = quest;
            btn.onClick.AddListener(() => _dispatchPanel.Open(capturedQuest));

            return go;
        }

        // ── Active Missions ───────────────────────────────────────────────

        private void RefreshMissions()
        {
            foreach (var r in _missionRows) Destroy(r);
            _missionRows.Clear();

            var missions = _missionMgr.ActiveMissions;
            if (missions.Count == 0)
            {
                _missionRows.Add(MakeEmptyRow(_missionContent, "No active missions"));
                return;
            }
            foreach (var m in missions)
            {
                string eta  = m.CurrentETA.ToETAString(_timeMgr.CurrentTime);
                string text = $"{m.Quest.Title}\n{m.StatusText}  ETA: {eta}";
                Color  col  = m.State == ActiveMissionState.TravelingBack
                    ? new Color(0.4f, 0.8f, 0.4f) : Color.white;
                _missionRows.Add(MakeTextRow(_missionContent, text, 13f, col, 52f));
            }
        }

        // ── Roster ────────────────────────────────────────────────────────

        private void RefreshRoster()
        {
            foreach (Transform child in _rosterContent) Destroy(child.gameObject);

            // Header
            var h = UIBuilder.Row(_rosterContent, new Color(0.18f, 0.18f, 0.32f, 1f), 26f);
            UIBuilder.RowCell(h.transform, "Name",    12f, Color.yellow, new Vector2(0f,    0f), new Vector2(0.23f, 1f));
            UIBuilder.RowCell(h.transform, "Job",     12f, Color.yellow, new Vector2(0.23f, 0f), new Vector2(0.40f, 1f));
            UIBuilder.RowCell(h.transform, "Rank",    12f, Color.yellow, new Vector2(0.40f, 0f), new Vector2(0.53f, 1f));
            UIBuilder.RowCell(h.transform, "Status",  12f, Color.yellow, new Vector2(0.53f, 0f), new Vector2(0.70f, 1f));
            UIBuilder.RowCell(h.transform, "Gold",    11f, Color.yellow, new Vector2(0.70f, 0f), new Vector2(0.82f, 1f));
            UIBuilder.RowCell(h.transform, "Str/Vit", 10f, Color.yellow, new Vector2(0.82f, 0f), new Vector2(1f,    1f));

            foreach (var adv in _guild.Adventurers)
            {
                var bgCol = adv.IsAvailable
                    ? new Color(0.1f, 0.15f, 0.25f, 1f)
                    : new Color(0.15f, 0.1f, 0.1f, 1f);
                var row = UIBuilder.Row(_rosterContent, bgCol, 34f);

                Color statusCol = adv.IsAvailable
                    ? new Color(0.4f, 0.9f, 0.4f)
                    : new Color(0.9f, 0.5f, 0.3f);

                UIBuilder.RowCell(row.transform, adv.Name,
                    13f, Color.white, new Vector2(0f, 0f), new Vector2(0.23f, 1f));
                UIBuilder.RowCell(row.transform, AdventurerJobInfo.DisplayName(adv.Job),
                    12f, Color.white, new Vector2(0.23f, 0f), new Vector2(0.40f, 1f));
                UIBuilder.RowCell(row.transform, AdventurerRankInfo.DisplayName(adv.Rank),
                    12f, AdventurerRankInfo.BadgeColor(adv.Rank), new Vector2(0.40f, 0f), new Vector2(0.53f, 1f));
                UIBuilder.RowCell(row.transform, adv.StatusText,
                    12f, statusCol, new Vector2(0.53f, 0f), new Vector2(0.70f, 1f));
                UIBuilder.RowCell(row.transform, $"{adv.EarnedGold}G",
                    11f, new Color(1f, 0.85f, 0.35f), new Vector2(0.70f, 0f), new Vector2(0.82f, 1f));
                UIBuilder.RowCell(row.transform, $"S{adv.Stats.Strength} V{adv.Stats.Vitality}",
                    10f, Color.white, new Vector2(0.82f, 0f), new Vector2(1f, 1f));
            }
        }

        // ─── Row helpers ──────────────────────────────────────────────────

        private static GameObject MakeEmptyRow(Transform parent, string text)
        {
            var go = UIBuilder.Row(parent, new Color(0f, 0f, 0f, 0.3f), 40f);
            UIBuilder.AddLabel(go.transform, text, 13f,
                new Color(0.6f, 0.6f, 0.6f), TextAlignmentOptions.Center);
            return go;
        }

        private static GameObject MakeTextRow(Transform parent, string text,
            float fontSize, Color color, float height)
        {
            var go = UIBuilder.Row(parent, new Color(0.1f, 0.1f, 0.2f, 0.8f), height);
            UIBuilder.AddLabel(go.transform, text, fontSize, color, TextAlignmentOptions.MidlineLeft);
            return go;
        }
    }
}
