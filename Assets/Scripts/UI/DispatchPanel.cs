using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DQSim
{
    public class DispatchPanel : MonoBehaviour
    {
        private TextMeshProUGUI _questInfoText;
        private RectTransform   _advContent;
        private Button          _dispatchBtn;
        private TextMeshProUGUI _dispatchBtnText;

        private GuildManager _guild;
        private Quest _currentQuest;
        private readonly List<(Adventurer adv, Toggle toggle)> _rows =
            new List<(Adventurer, Toggle)>();

        private void Awake()
        {
            var topBar = UIBuilder.TopBar(transform, 34, new Color(0.1f, 0.1f, 0.3f, 1f));
            UIBuilder.AddLabel(topBar.transform, "Dispatch Party", 15,
                new Color(0.9f, 0.8f, 0.3f), TextAlignmentOptions.Center);

            // Quest info text
            var infoGO = new GameObject("QuestInfo");
            infoGO.transform.SetParent(transform, false);
            _questInfoText           = infoGO.AddComponent<TextMeshProUGUI>();
            _questInfoText.fontSize  = 13f;
            _questInfoText.color     = Color.white;
            _questInfoText.alignment = TextAlignmentOptions.TopLeft;
            var infoRT               = infoGO.GetComponent<RectTransform>();
            infoRT.anchorMin         = new Vector2(0, 1);
            infoRT.anchorMax         = new Vector2(1, 1);
            infoRT.pivot             = new Vector2(0.5f, 1);
            infoRT.anchoredPosition  = new Vector2(0, -38);
            infoRT.sizeDelta         = new Vector2(-16, 86);

            // Divider label
            var divGO = new GameObject("Divider");
            divGO.transform.SetParent(transform, false);
            var divTMP       = divGO.AddComponent<TextMeshProUGUI>();
            divTMP.text      = "-- Select Party --";
            divTMP.fontSize  = 12f;
            divTMP.color     = new Color(0.7f, 0.7f, 0.7f);
            divTMP.alignment = TextAlignmentOptions.Center;
            var divRT        = divGO.GetComponent<RectTransform>();
            divRT.anchorMin  = new Vector2(0, 1);
            divRT.anchorMax  = new Vector2(1, 1);
            divRT.pivot      = new Vector2(0.5f, 1);
            divRT.anchoredPosition = new Vector2(0, -126);
            divRT.sizeDelta        = new Vector2(0, 22);

            _advContent = UIBuilder.ScrollContent(transform, topOffset: 150, bottomOffset: 58);

            var cancelBtn = UIBuilder.HalfBtn(transform, "Cancel", 14,
                new Color(0.45f, 0.15f, 0.15f), left: true);

            _dispatchBtn     = UIBuilder.HalfBtn(transform, "Dispatch", 14,
                new Color(0.2f, 0.4f, 0.8f), left: false);
            _dispatchBtnText = _dispatchBtn.GetComponentInChildren<TextMeshProUGUI>();

            cancelBtn.onClick.AddListener(() => gameObject.SetActive(false));
        }

        public void Initialize(GuildManager guild)
        {
            _guild = guild;
            _dispatchBtn.onClick.AddListener(OnDispatch);
            gameObject.SetActive(false);
        }

        public void Open(Quest quest)
        {
            _currentQuest          = quest;
            _questInfoText.text    =
                $"<b>{quest.Title}</b>\n" +
                $"Location: {quest.LocationName}   Rank: {AdventurerRankInfo.DisplayName(quest.MinRank)}+\n" +
                $"Reward: {quest.RewardGold}G   Party: {quest.MinPartySize}-{quest.MaxPartySize}";

            BuildAdventurerList();
            gameObject.SetActive(true);
        }

        private void BuildAdventurerList()
        {
            foreach (Transform child in _advContent) Destroy(child.gameObject);
            _rows.Clear();

            foreach (var adv in _guild.Adventurers)
                CreateAdventurerRow(adv).transform.SetParent(_advContent, false);

            UpdateDispatchButton();
        }

        private GameObject CreateAdventurerRow(Adventurer adv)
        {
            var go = new GameObject("AdvRow");
            go.AddComponent<Image>().color = adv.IsAvailable
                ? new Color(0.1f, 0.15f, 0.25f, 1f)
                : new Color(0.1f, 0.1f, 0.1f, 0.5f);
            var le = go.AddComponent<LayoutElement>();
            le.preferredHeight = 42f;

            // Toggle
            var toggleGO  = new GameObject("Toggle");
            toggleGO.transform.SetParent(go.transform, false);
            var toggle    = toggleGO.AddComponent<Toggle>();
            toggle.interactable = adv.IsAvailable;

            var tRT = toggleGO.GetComponent<RectTransform>();
            tRT.anchorMin        = new Vector2(0, 0.5f);
            tRT.anchorMax        = new Vector2(0, 0.5f);
            tRT.anchoredPosition = new Vector2(20, 0);
            tRT.sizeDelta        = new Vector2(20, 20);

            var bgImg = toggleGO.AddComponent<Image>();
            bgImg.color         = new Color(0.3f, 0.3f, 0.3f, 1f);
            toggle.targetGraphic = bgImg;

            var checkGO  = new GameObject("Checkmark");
            checkGO.transform.SetParent(toggleGO.transform, false);
            var checkImg = checkGO.AddComponent<Image>();
            checkImg.color = new Color(0.2f, 0.8f, 0.2f, 1f);
            var checkRT  = checkGO.GetComponent<RectTransform>();
            checkRT.anchorMin = new Vector2(0.15f, 0.15f);
            checkRT.anchorMax = new Vector2(0.85f, 0.85f);
            checkRT.offsetMin = Vector2.zero;
            checkRT.offsetMax = Vector2.zero;
            toggle.graphic    = checkImg;

            toggle.isOn = false;
            toggle.onValueChanged.AddListener(_ => UpdateDispatchButton());

            // Label
            var labelGO  = new GameObject("Label");
            labelGO.transform.SetParent(go.transform, false);
            var tmp      = labelGO.AddComponent<TextMeshProUGUI>();
            tmp.text      = adv.IsAvailable ? adv.ShortLabel : $"[On Quest] {adv.Name}";
            tmp.fontSize  = 13f;
            tmp.color     = adv.IsAvailable ? Color.white : new Color(0.5f, 0.5f, 0.5f);
            tmp.alignment = TextAlignmentOptions.MidlineLeft;
            var labelRT  = labelGO.GetComponent<RectTransform>();
            labelRT.anchorMin = new Vector2(0, 0);
            labelRT.anchorMax = new Vector2(1, 1);
            labelRT.offsetMin = new Vector2(46, 2);
            labelRT.offsetMax = new Vector2(-6, -2);

            if (adv.IsAvailable) _rows.Add((adv, toggle));

            return go;
        }

        private void UpdateDispatchButton()
        {
            int  selected    = GetSelectedParty().Count;
            bool canDispatch = _currentQuest != null &&
                               selected >= _currentQuest.MinPartySize &&
                               selected <= _currentQuest.MaxPartySize;
            _dispatchBtn.interactable = canDispatch;
            if (_currentQuest != null)
                _dispatchBtnText.text = $"Dispatch ({selected}/{_currentQuest.MaxPartySize})";
        }

        private List<Adventurer> GetSelectedParty()
        {
            var list = new List<Adventurer>();
            foreach (var (adv, toggle) in _rows)
                if (toggle.isOn) list.Add(adv);
            return list;
        }

        private void OnDispatch()
        {
            var party = GetSelectedParty();
            if (!_guild.CanDispatch(_currentQuest, party)) return;
            _guild.DispatchParty(_currentQuest, party);
            gameObject.SetActive(false);
        }
    }
}
