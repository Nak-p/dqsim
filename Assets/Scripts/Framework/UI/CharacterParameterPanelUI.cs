// Assets/Scripts/Framework/UI/CharacterParameterPanelUI.cs
// AgentSim — キャラクターパラメータ表示パネル（IMGUI）
//
// DispatchManager のロスターを左ペインに一覧表示し、
// 選択したキャラクターの詳細パラメータを右ペインに表示する。
//
// UI ラベルはすべて SettingsRegistry.Current.Game から取得する。
// C# にゲーム用語をハードコーディングしてはいけない。

using System.Collections.Generic;
using UnityEngine;
using AgentSim.Config;
using AgentSim.Core;

namespace AgentSim.UI
{
    public class CharacterParameterPanelUI : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────
        [SerializeField] private DispatchManager dispatchManager;

        // ── パネルサイズ（視覚的アルゴリズム用定数、ゲームパラメータではない） ──
        private const int PanelWidth   = 720;
        private const int PanelHeight  = 520;
        private const int ListWidth    = 210;
        private const int HeaderHeight = 28;
        private const int RowHeight    = 22;

        // ── 状態 ──────────────────────────────────────────────────────
        private bool    _visible       = true;
        private int     _selectedIndex = -1;
        private Vector2 _rosterScroll;
        private bool    _initialized   = false;

        // derived stat キャッシュ（OnStateChanged 時のみ再計算）
        private readonly Dictionary<string, int> _derivedCache = new Dictionary<string, int>();
        private int _cachedTotalPower;

        // ── Unity ライフサイクル ──────────────────────────────────────
        private void Start()
        {
            // Inspector 未設定の場合はシーン内から自動検索
            if (dispatchManager == null)
                dispatchManager = FindFirstObjectByType<DispatchManager>();

            if (dispatchManager != null)
                SetupSubscription();
        }

        private void SetupSubscription()
        {
            if (_initialized) return;
            _initialized = true;

            dispatchManager.OnStateChanged += OnStateChanged;

            // 既にロスターが生成済みの場合も正しく表示されるよう即時更新
            OnStateChanged();
        }

        // ── 公開 API（GameBootstrap からの明示的初期化用） ────────────
        /// <summary>
        /// GameBootstrap から呼ぶ場合はこちら。
        /// 呼ばない場合は Start() でシーン内から自動検索する。
        /// </summary>
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

        // ── イベントハンドラ ──────────────────────────────────────────
        private void OnStateChanged()
        {
            // 選択インデックスが範囲外になっていたらリセット
            int rosterCount = dispatchManager?.Roster?.Count ?? 0;
            if (_selectedIndex >= rosterCount)
                _selectedIndex = rosterCount > 0 ? rosterCount - 1 : -1;

            // 選択中キャラクターの derived stat を再キャッシュ
            RebuildDerivedCache();
        }

        private void RebuildDerivedCache()
        {
            _derivedCache.Clear();
            _cachedTotalPower = 0;

            if (_selectedIndex < 0) return;
            if (dispatchManager?.Roster == null) return;
            if (_selectedIndex >= dispatchManager.Roster.Count) return;

            var character = dispatchManager.Roster[_selectedIndex];
            if (character?.Stats == null) return;

            var statDefs = SettingsRegistry.Current?.Stats;
            if (statDefs == null) return;

            foreach (var def in statDefs.derived_stats)
                _derivedCache[def.id] = character.Stats.GetDerived(def.id);

            _cachedTotalPower = character.Stats.TotalPower;
        }

        // ── OnGUI ─────────────────────────────────────────────────────
        private void OnGUI()
        {
            // Tab キーでパネルのトグル
            if (Event.current.type == EventType.KeyDown &&
                Event.current.keyCode == KeyCode.Tab)
            {
                _visible = !_visible;
                Event.current.Use();
            }

            if (!_visible) return;
            if (SettingsRegistry.Current == null) return;

            // DispatchManager がまだ見つかっていない場合は再試行
            if (dispatchManager == null)
            {
                dispatchManager = FindFirstObjectByType<DispatchManager>();
                if (dispatchManager != null) SetupSubscription();
                else return;
            }

            // 画面中央にパネルを配置
            float px = (Screen.width  - PanelWidth)  * 0.5f;
            float py = (Screen.height - PanelHeight) * 0.5f;
            var panelRect = new Rect(px, py, PanelWidth, PanelHeight);

            GUI.Box(panelRect, GUIContent.none, UIStyleProvider.PanelBackground);

            DrawHeader(new Rect(px, py, PanelWidth, HeaderHeight));
            DrawRosterList(new Rect(px, py + HeaderHeight, ListWidth, PanelHeight - HeaderHeight));
            DrawDetailPanel(new Rect(px + ListWidth, py + HeaderHeight,
                                     PanelWidth - ListWidth, PanelHeight - HeaderHeight));
        }

        // ── ヘッダー ──────────────────────────────────────────────────
        private void DrawHeader(Rect r)
        {
            string title = SettingsRegistry.Current?.Game?.characters_term ?? "Characters";
            GUI.Label(new Rect(r.x, r.y, r.width - 80f, r.height), title,
                      UIStyleProvider.TitleStyle);
            GUI.Label(new Rect(r.x + r.width - 80f, r.y, 76f, r.height),
                      "[Tab: hide]", UIStyleProvider.LabelStyle);
        }

        // ── ロスター一覧（左ペイン） ──────────────────────────────────
        private void DrawRosterList(Rect r)
        {
            var roster = dispatchManager?.Roster;
            if (roster == null || roster.Count == 0)
            {
                GUI.Label(r, " (no characters)", UIStyleProvider.LabelStyle);
                return;
            }

            float contentHeight = roster.Count * RowHeight + 4f;
            var scrollContent   = new Rect(0, 0, r.width - 16f, contentHeight);

            _rosterScroll = GUI.BeginScrollView(r, _rosterScroll, scrollContent);

            for (int i = 0; i < roster.Count; i++)
            {
                bool isSelected = (i == _selectedIndex);
                var  rowRect    = new Rect(0, i * RowHeight, scrollContent.width, RowHeight);
                var  style      = isSelected ? UIStyleProvider.SelectedRow : UIStyleProvider.NormalRow;

                if (GUI.Button(rowRect, roster[i].ShortLabel, style))
                {
                    _selectedIndex = i;
                    RebuildDerivedCache();
                }
            }

            GUI.EndScrollView();
        }

        // ── 詳細パネル（右ペイン） ────────────────────────────────────
        private void DrawDetailPanel(Rect r)
        {
            if (_selectedIndex < 0 ||
                dispatchManager?.Roster == null ||
                _selectedIndex >= dispatchManager.Roster.Count)
            {
                GUI.Label(r, "  Select a character", UIStyleProvider.LabelStyle);
                return;
            }

            CharacterDetailRenderer.Draw(r, dispatchManager.Roster[_selectedIndex],
                                         _derivedCache, _cachedTotalPower);
        }
    }
}
