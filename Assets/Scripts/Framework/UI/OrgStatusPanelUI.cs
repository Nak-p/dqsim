// Assets/Scripts/Framework/UI/OrgStatusPanelUI.cs
// AgentSim - 企業ステータス UI パネル (IMGUI)
// 企業の財務・ロスター・ミッション状況と設定値を表示する。
// UI ラベルはすべて SettingsRegistry.Current.Game から取得する。
// C# にゲーム用語をハードコーディングしてはいけない。

using UnityEngine;
using AgentSim.Config;
using AgentSim.Core;

namespace AgentSim.UI
{
    public class OrgStatusPanelUI : MonoBehaviour
    {
        [SerializeField] private DispatchManager dispatchManager;

        private const int   PanelWidth    = 500;
        private const int   PanelHeight   = 500;
        private const int   HeaderHeight  = 28;
        private const float LabelColWidth = 160f;
        private const float RowHeight     = 18f;
        private const float SectionGap    = 8f;
        private const float PanelMargin   = 20f;

        private bool  _visible     = false;
        private bool  _initialized = false;

        private int   _cachedAvailable;
        private int   _cachedTotal;
        private int   _cachedActiveMissions;
        private int   _cachedAvailableContracts;
        private int[] _cachedTierCounts;
        private string _cachedRankDisplayName = "-";
        private int    _cachedScale;
        private int    _cachedNextRankEarned  = -1;

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
            OnStateChanged();
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
            if (SettingsRegistry.Current == null) return;

            var tiers = SettingsRegistry.Current.Tiers?.tiers;
            if (tiers != null)
            {
                _cachedTierCounts = new int[tiers.Length];
                _cachedAvailable  = 0;
                _cachedTotal      = dispatchManager.Roster?.Count ?? 0;
                foreach (var ch in dispatchManager.Roster)
                {
                    if (ch.IsAvailable) _cachedAvailable++;
                    for (int i = 0; i < tiers.Length; i++)
                        if (ch.TierId == tiers[i].id) { _cachedTierCounts[i]++; break; }
                }
            }

            _cachedActiveMissions     = dispatchManager.ActiveMissions?.Count     ?? 0;
            _cachedAvailableContracts = dispatchManager.AvailableContracts?.Count ?? 0;

            var rank = SettingsRegistry.Current.GetCurrentOrgRank(dispatchManager.TotalOrgEarned);
            _cachedRankDisplayName = rank?.display_name ?? "-";
            _cachedScale           = rank?.scale ?? 0;
            _cachedNextRankEarned  = ComputeNextRankThreshold(dispatchManager.TotalOrgEarned);
        }

        private void OnGUI()
        {
            if (Event.current.type == EventType.KeyDown &&
                Event.current.keyCode == KeyCode.O)
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

            var panelRect = new Rect(PanelMargin, PanelMargin, PanelWidth, PanelHeight);
            GUI.Box(panelRect, GUIContent.none, UIStyleProvider.PanelBackground);
            DrawHeader(new Rect(PanelMargin, PanelMargin, PanelWidth, HeaderHeight));
            DrawBody(new Rect(PanelMargin, PanelMargin + HeaderHeight,
                              PanelWidth, PanelHeight - HeaderHeight));
        }

        private void DrawHeader(Rect r)
        {
            var    game  = SettingsRegistry.Current.Game;
            string title = game.organization_name + "  -  " + game.organization_label;
            GUI.Label(new Rect(r.x + 8f, r.y, r.width - 80f, r.height),
                      title, UIStyleProvider.TitleStyle);
            GUI.Label(new Rect(r.x + r.width - 76f, r.y, 72f, r.height),
                      "[O: hide]", UIStyleProvider.LabelStyle);
        }

        private void DrawBody(Rect r)
        {
            float x = r.x + 8f;
            float y = r.y + SectionGap;
            float w = r.width - 16f;

            DrawOrgStatus(x, ref y, w);
            y += SectionGap;
            DrawFinances(x, ref y, w);
            y += SectionGap;
            DrawRosterStatus(x, ref y, w);
            y += SectionGap;
            DrawOperations(x, ref y, w);
            y += SectionGap;
            DrawConfiguration(x, ref y, w);
        }

        private void DrawOrgStatus(float x, ref float y, float w)
        {
            var lStyle = UIStyleProvider.LabelStyle;
            var vStyle = UIStyleProvider.ValueStyle;
            var hStyle = UIStyleProvider.SectionHeader;

            GUI.Label(new Rect(x, y, w, RowHeight), "-- Status --", hStyle);
            y += RowHeight;

            DrawRow(x, ref y, w, LabelColWidth, RowHeight,
                    "Rank", _cachedRankDisplayName, lStyle, vStyle);
            DrawRow(x, ref y, w, LabelColWidth, RowHeight,
                    "Scale", _cachedScale.ToString(), lStyle, vStyle);

            if (_cachedNextRankEarned >= 0)
            {
                var game = SettingsRegistry.Current.Game;
                DrawRow(x, ref y, w, LabelColWidth, RowHeight,
                        "Next Rank at",
                        _cachedNextRankEarned + game.currency_symbol, lStyle, lStyle);
            }
        }

        private int ComputeNextRankThreshold(int totalEarned)
        {
            var ranks = SettingsRegistry.Current.OrgRanks?.ranks;
            if (ranks == null) return -1;
            foreach (var r in ranks)
                if (r.min_earned > totalEarned) return r.min_earned;
            return -1;
        }

        private void DrawFinances(float x, ref float y, float w)
        {
            var game   = SettingsRegistry.Current.Game;
            var lStyle = UIStyleProvider.LabelStyle;
            var vStyle = UIStyleProvider.ValueStyle;
            var hStyle = UIStyleProvider.SectionHeader;
            string sym = game.currency_symbol;

            GUI.Label(new Rect(x, y, w, RowHeight), "-- Finances --", hStyle);
            y += RowHeight;

            DrawRow(x, ref y, w, LabelColWidth, RowHeight,
                    game.currency_name,
                    dispatchManager.OrgCurrency + sym, lStyle, vStyle);
            DrawRow(x, ref y, w, LabelColWidth, RowHeight,
                    "Total Earned",
                    dispatchManager.TotalOrgEarned + sym, lStyle, vStyle);
            DrawRow(x, ref y, w, LabelColWidth, RowHeight,
                    "Org Share",
                    game.org_share_percent + "%", lStyle, vStyle);
        }

        private void DrawRosterStatus(float x, ref float y, float w)
        {
            var game   = SettingsRegistry.Current.Game;
            var lStyle = UIStyleProvider.LabelStyle;
            var vStyle = UIStyleProvider.ValueStyle;
            var hStyle = UIStyleProvider.SectionHeader;

            GUI.Label(new Rect(x, y, w, RowHeight),
                      "-- " + game.characters_term + " --", hStyle);
            y += RowHeight;

            DrawRow(x, ref y, w, LabelColWidth, RowHeight,
                    "Available",
                    _cachedAvailable + " / " + _cachedTotal, lStyle, vStyle);

            var tiers = SettingsRegistry.Current.Tiers?.tiers;
            if (tiers != null && _cachedTierCounts != null)
            {
                for (int i = 0; i < tiers.Length && i < _cachedTierCounts.Length; i++)
                    DrawRow(x, ref y, w, LabelColWidth, RowHeight,
                            "  " + tiers[i].display_name,
                            _cachedTierCounts[i].ToString(), lStyle, lStyle);
            }
        }

        private void DrawOperations(float x, ref float y, float w)
        {
            var game   = SettingsRegistry.Current.Game;
            var lStyle = UIStyleProvider.LabelStyle;
            var vStyle = UIStyleProvider.ValueStyle;
            var hStyle = UIStyleProvider.SectionHeader;

            GUI.Label(new Rect(x, y, w, RowHeight), "-- Operations --", hStyle);
            y += RowHeight;

            DrawRow(x, ref y, w, LabelColWidth, RowHeight,
                    "Active " + game.dispatch_term,
                    _cachedActiveMissions.ToString(), lStyle, vStyle);
            DrawRow(x, ref y, w, LabelColWidth, RowHeight,
                    game.contracts_term,
                    _cachedAvailableContracts + " / " + game.max_available_contracts,
                    lStyle, vStyle);
        }

        private void DrawConfiguration(float x, ref float y, float w)
        {
            var game   = SettingsRegistry.Current.Game;
            var lStyle = UIStyleProvider.LabelStyle;
            var vStyle = UIStyleProvider.ValueStyle;
            var hStyle = UIStyleProvider.SectionHeader;

            GUI.Label(new Rect(x, y, w, RowHeight), "-- Configuration --", hStyle);
            y += RowHeight;

            DrawRow(x, ref y, w, LabelColWidth, RowHeight,
                    "Max Party Size",
                    game.max_party_size.ToString(), lStyle, vStyle);
            DrawRow(x, ref y, w, LabelColWidth, RowHeight,
                    "Roster Capacity",
                    game.roster_size.ToString(), lStyle, vStyle);
            DrawRow(x, ref y, w, LabelColWidth, RowHeight,
                    "Contract Interval (days)",
                    game.contract_gen_interval_days.ToString("F1"), lStyle, vStyle);
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


