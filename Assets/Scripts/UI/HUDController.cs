using TMPro;
using UnityEngine;

namespace DQSim
{
    public class HUDController : MonoBehaviour
    {
        private TextMeshProUGUI _dateTimeLabel;
        private TextMeshProUGUI _goldLabel;
        private TextMeshProUGUI _activeMissionsLabel;

        private TimeManager _timeManager;
        private GuildManager _guild;
        private MissionManager _missionManager;

        private void Awake()
        {
            _dateTimeLabel       = MakeLabel("DateTimeLabel", 20, -8f,  28f);
            _goldLabel           = MakeLabel("GoldLabel",     15, -40f, 22f);
            _activeMissionsLabel = MakeLabel("ActiveLabel",   15, -64f, 22f);
        }

        public void Initialize(TimeManager timeManager, GuildManager guild, MissionManager missionManager)
        {
            _timeManager    = timeManager;
            _guild          = guild;
            _missionManager = missionManager;

            _timeManager.OnTimeChanged       += t => _dateTimeLabel.text = t.ToString();
            _guild.OnGuildStateChanged       += RefreshGuildInfo;
            _missionManager.OnReturnedToBase += _ => RefreshGuildInfo();

            RefreshGuildInfo();
        }

        private void RefreshGuildInfo()
        {
            _goldLabel.text           = $"Gold: {_guild.GuildGold}G";
            _activeMissionsLabel.text = $"Active: {_missionManager.ActiveMissions.Count} missions";
        }

        private TextMeshProUGUI MakeLabel(string goName, float fontSize, float anchoredY, float height)
        {
            var go = new GameObject(goName);
            go.transform.SetParent(transform, false);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.fontSize  = fontSize;
            tmp.color     = Color.white;
            tmp.alignment = TextAlignmentOptions.TopLeft;
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin        = new Vector2(0, 1);
            rt.anchorMax        = new Vector2(1, 1);
            rt.pivot            = new Vector2(0.5f, 1);
            rt.anchoredPosition = new Vector2(0, anchoredY);
            rt.sizeDelta        = new Vector2(-16, height);
            return tmp;
        }
    }
}
