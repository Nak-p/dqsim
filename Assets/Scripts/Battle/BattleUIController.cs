using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace DQSim.Battle
{
    public class BattleUIController : MonoBehaviour
    {
        public BattleController battleController;
        
        private TextMeshProUGUI _statusLabel;
        private Button _spawnBtn;
        private Button _resetMpBtn;

        private void Awake()
        {
            SetupUI();
        }

        private void SetupUI()
        {
            var canvasGo = new GameObject("BattleTestUI");
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGo.AddComponent<CanvasScaler>();
            canvasGo.AddComponent<GraphicRaycaster>();

            // Panel
            var panel = new GameObject("Panel");
            panel.transform.SetParent(canvasGo.transform, false);
            var rt = panel.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(1, 1);
            rt.anchorMax = new Vector2(1, 1);
            rt.pivot = new Vector2(1, 1);
            rt.anchoredPosition = new Vector2(-10, -10);
            rt.sizeDelta = new Vector2(200, 150);
            panel.AddComponent<Image>().color = new Color(0, 0, 0, 0.5f);

            // Status Label
            _statusLabel = MakeLabel("StatusLabel", panel.transform, 14, -10, 60);
            
            // Spawn Button
            _spawnBtn = MakeButton("SpawnBtn", "Spawn Unit", panel.transform, -80);
            _spawnBtn.onClick.AddListener(() => battleController.SpawnUnit());

            // Reset MP Button
            _resetMpBtn = MakeButton("ResetMpBtn", "Reset MP", panel.transform, -115);
            _resetMpBtn.onClick.AddListener(() => battleController.ResetUnitMP());
        }

        private void Update()
        {
            if (battleController.activeUnit != null)
            {
                var unit = battleController.activeUnit;
                _statusLabel.text = $"Unit: ({unit.HexPosition.x}, {unit.HexPosition.y})\nMP: {unit.CurrentMP:F1}/{unit.MaxMP:F1}";
                _spawnBtn.interactable = false;
            }
            else
            {
                _statusLabel.text = "No Unit Spawned";
                _spawnBtn.interactable = true;
            }
        }

        private TextMeshProUGUI MakeLabel(string name, Transform parent, float fontSize, float anchoredY, float height)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.fontSize = fontSize;
            tmp.color = Color.white;
            tmp.alignment = TextAlignmentOptions.TopLeft;
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(1, 1);
            rt.pivot = new Vector2(0.5f, 1);
            rt.anchoredPosition = new Vector2(0, anchoredY);
            rt.sizeDelta = new Vector2(-10, height);
            return tmp;
        }

        private Button MakeButton(string name, string label, Transform parent, float anchoredY)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.color = Color.gray;
            var btn = go.AddComponent<Button>();
            
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(1, 1);
            rt.pivot = new Vector2(0.5f, 1);
            rt.anchoredPosition = new Vector2(0, anchoredY);
            rt.sizeDelta = new Vector2(-10, 30);

            var txtGo = new GameObject("Text");
            txtGo.transform.SetParent(go.transform, false);
            var tmp = txtGo.AddComponent<TextMeshProUGUI>();
            tmp.text = label;
            tmp.fontSize = 14;
            tmp.color = Color.black;
            tmp.alignment = TextAlignmentOptions.Center;
            var txtRt = txtGo.GetComponent<RectTransform>();
            txtRt.anchorMin = Vector2.zero;
            txtRt.anchorMax = Vector2.one;
            txtRt.sizeDelta = Vector2.zero;

            return btn;
        }
    }
}
