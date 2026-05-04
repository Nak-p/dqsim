// Assets/Scripts/Framework/UI/ContractBoardUI.cs
// AgentSim - 依頼掲示板 UI パネル (IMGUI)
// DispatchManager.AvailableContracts を一覧表示する。
// UI ラベルはすべて SettingsRegistry.Current.Game から取得する。
// C# にゲーム用語をハードコーディングしてはいけない。

using UnityEngine;
using AgentSim.Config;
using AgentSim.Core;

namespace AgentSim.UI
{
    public class ContractBoardUI : MonoBehaviour
    {
        [SerializeField] private DispatchManager dispatchManager;

        private const int PanelWidth   = 640;
        private const int PanelHeight  = 420;
        private const int ListWidth    = 200;
        private const int HeaderHeight = 28;
        private const int RowHeight    = 22;

        private bool    _visible       = false;
        private int     _selectedIndex = -1;
        private Vector2 _listScroll;
        private bool    _initialized   = false;

        private void Start()
        {
            if (dispatchManager == null)
                dispatchManager = FindFirstObjectByType<DispatchManager>();
            if (dispatchManager != null) SetupSubscription();
        }

        private void SetupSubscription()
        {
            if (_initialized) return;
            _initialized = true;
            dispatchManager.OnStateChanged += OnStateChanged;
        }

        public void Initialize(DispatchManager dm)
        {
            dispatchManager = dm;
            SetupSubscription();
        }

        private void OnDestroy()
        {
            if (dispatchManager != null)
                dispatchManager.OnStateChanged -= OnStateChanged;
        }

        private void OnStateChanged()
        {
            int count = dispatchManager?.AvailableContracts?.Count ?? 0;
            if (_selectedIndex >= count)
                _selectedIndex = count > 0 ? count - 1 : -1;
        }

        private void OnGUI()
        {
            if (Event.current.type == EventType.KeyDown &&
                Event.current.keyCode == KeyCode.B)
            {
                _visible = !_visible;
                Event.current.Use();
            }
            if (!_visible) return;
            if (SettingsRegistry.Current == null) return;

            if (dispatchManager == null)
            {
                dispatchManager = FindFirstObjectByType<DispatchManager>();
                if (dispatchManager != null) SetupSubscription();
                else return;
            }

            float px = (Screen.width  - PanelWidth)  * 0.5f;
            float py = (Screen.height - PanelHeight) * 0.35f;

            GUI.Box(new Rect(px, py, PanelWidth, PanelHeight),
                    GUIContent.none, UIStyleProvider.PanelBackground);
            DrawHeader(new Rect(px, py, PanelWidth, HeaderHeight));
            DrawContractList(new Rect(px, py + HeaderHeight,
                                      ListWidth, PanelHeight - HeaderHeight));
            DrawDetailPanel(new Rect(px + ListWidth, py + HeaderHeight,
                                     PanelWidth - ListWidth, PanelHeight - HeaderHeight));
        }

        private void DrawHeader(Rect r)
        {
            var    game  = SettingsRegistry.Current.Game;
            int    count = dispatchManager?.AvailableContracts?.Count ?? 0;
            string title = game.contracts_term + "  (" + count + ")";
            GUI.Label(new Rect(r.x, r.y, r.width - 80f, r.height),
                      title, UIStyleProvider.TitleStyle);
            GUI.Label(new Rect(r.x + r.width - 80f, r.y, 76f, r.height),
                      "[B: hide]", UIStyleProvider.LabelStyle);
        }

        private void DrawContractList(Rect r)
        {
            var contracts = dispatchManager?.AvailableContracts;
            if (contracts == null || contracts.Count == 0)
            {
                string none = SettingsRegistry.Current?.Game?.contracts_term ?? "contracts";
                GUI.Label(r, " (no " + none + ")", UIStyleProvider.LabelStyle);
                return;
            }

            float contentH = contracts.Count * RowHeight + 4f;
            var scrollArea = new Rect(0, 0, r.width - 16f, contentH);
            _listScroll = GUI.BeginScrollView(r, _listScroll, scrollArea);

            for (int i = 0; i < contracts.Count; i++)
            {
                var  c     = contracts[i];
                bool sel   = (i == _selectedIndex);
                var  row   = new Rect(0, i * RowHeight, scrollArea.width, RowHeight);
                var  style = sel ? UIStyleProvider.SelectedRow : UIStyleProvider.NormalRow;
                string tierLabel = SettingsRegistry.Current.GetTier(c.MinTierId)?.display_name
                                   ?? c.MinTierId;
                if (GUI.Button(row, c.Title + "  [" + tierLabel + "]", style))
                    _selectedIndex = i;
            }

            GUI.EndScrollView();
        }

        private void DrawDetailPanel(Rect r)
        {
            var contracts = dispatchManager?.AvailableContracts;
            if (_selectedIndex < 0 || contracts == null || _selectedIndex >= contracts.Count)
            {
                string hint = SettingsRegistry.Current?.Game?.contract_term ?? "contract";
                GUI.Label(r, "  Select a " + hint, UIStyleProvider.LabelStyle);
                return;
            }

            var   c      = contracts[_selectedIndex];
            var   game   = SettingsRegistry.Current.Game;
            var   lStyle = UIStyleProvider.LabelStyle;
            var   vStyle = UIStyleProvider.ValueStyle;
            var   hStyle = UIStyleProvider.SectionHeader;
            float x  = r.x + 8f;
            float y  = r.y + 8f;
            float w  = r.width - 16f;
            float lw = 90f;
            float rh = 18f;

            GUI.Label(new Rect(x, y, w, rh + 2), c.Title, UIStyleProvider.TitleStyle);
            y += rh + 6f;

            DrawRow(x, ref y, w, lw, rh, "Location",        c.LocationName,                      lStyle, vStyle);
            DrawRow(x, ref y, w, lw, rh, game.currency_name, c.Reward.ToString() + game.currency_symbol, lStyle, vStyle);

            string tierName = SettingsRegistry.Current.GetTier(c.MinTierId)?.display_name
                              ?? c.MinTierId;
            DrawRow(x, ref y, w, lw, rh, "Min Tier", tierName, lStyle, vStyle);

            string partyText = c.MinPartySize == c.MaxPartySize
                ? c.MinPartySize.ToString()
                : c.MinPartySize + " - " + c.MaxPartySize;
            DrawRow(x, ref y, w, lw, rh, "Party", partyText, lStyle, vStyle);
            DrawRow(x, ref y, w, lw, rh, "Dest",
                    "(" + c.Destination.x + ", " + c.Destination.y + ")", lStyle, lStyle);

            y += 8f;
            GUI.Label(new Rect(x, y, w, rh), "--- Description ---", hStyle);
            y += rh;

            var descStyle      = new GUIStyle(UIStyleProvider.LabelStyle);
            descStyle.wordWrap = true;
            GUI.Label(new Rect(x, y, w, r.height - (y - r.y) - 8f), c.Description, descStyle);
        }

        private static void DrawRow(
            float x, ref float y, float w, float lw, float rh,
            string label, string value, GUIStyle ls, GUIStyle vs)
        {
            GUI.Label(new Rect(x,      y, lw,     rh), label, ls);
            GUI.Label(new Rect(x + lw, y, w - lw, rh), value, vs);
            y += rh;
        }
    }
}
