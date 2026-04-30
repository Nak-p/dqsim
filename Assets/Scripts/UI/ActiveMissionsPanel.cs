using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DQSim
{
    public class ActiveMissionsPanel : MonoBehaviour
    {
        private RectTransform _content;

        private MissionManager _missionManager;
        private TimeManager _timeManager;
        private readonly List<GameObject> _rows = new List<GameObject>();

        private void Awake()
        {
            var topBar = UIBuilder.TopBar(transform, 34, new Color(0.1f, 0.1f, 0.3f, 1f));
            UIBuilder.AddLabel(topBar.transform, "[ Active Missions ]", 15,
                new Color(0.9f, 0.8f, 0.3f), TextAlignmentOptions.Center);

            _content = UIBuilder.ScrollContent(transform, topOffset: 34, bottomOffset: 4);
        }

        public void Initialize(MissionManager missionManager, TimeManager timeManager)
        {
            _missionManager = missionManager;
            _timeManager    = timeManager;

            _timeManager.OnTimeChanged         += _ => RefreshRows();
            _missionManager.OnArrivalAtDestination += _ => RefreshRows();
            _missionManager.OnReturnedToBase       += _ => RefreshRows();

            RefreshRows();
        }

        private void RefreshRows()
        {
            foreach (var r in _rows) Destroy(r);
            _rows.Clear();

            var missions = _missionManager.ActiveMissions;
            if (missions.Count == 0)
            {
                var emptyRow = MakeRow("No active missions", Color.gray);
                emptyRow.transform.SetParent(_content, false);
                _rows.Add(emptyRow);
                return;
            }

            foreach (var m in missions)
            {
                string eta   = m.CurrentETA.ToETAString(_timeManager.CurrentTime);
                string label = $"{m.Quest.Title}\n{m.StatusText}  ETA: {eta}";
                Color  col   = m.State == ActiveMissionState.TravelingBack
                    ? new Color(0.4f, 0.8f, 0.4f) : Color.white;
                var row = MakeRow(label, col);
                row.transform.SetParent(_content, false);
                _rows.Add(row);
            }
        }

        private GameObject MakeRow(string text, Color textColor)
        {
            var go = UIBuilder.Row(_content, new Color(0.1f, 0.1f, 0.2f, 0.8f), 52f);
            UIBuilder.AddLabel(go.transform, text, 13f, textColor, TextAlignmentOptions.MidlineLeft);
            return go;
        }
    }
}
