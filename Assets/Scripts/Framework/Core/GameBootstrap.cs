// Assets/Scripts/Framework/Core/GameBootstrap.cs
// AgentSim — ゲーム起動エントリポイント
//
// シーンの最初の GameObject にアタッチする。
// settingId を変えるだけで世界観が切り替わる。

using UnityEngine;
using AgentSim.Config;
using AgentSim.Systems;
using AgentSim.UI;

namespace AgentSim.Core
{
    public class GameBootstrap : MonoBehaviour
    {
        // ── Inspector 設定 ────────────────────────────────────────────
        [Header("Setting")]
        [Tooltip("StreamingAssets/settings/ 以下のフォルダ名。これを変えるだけで世界観が切り替わる。")]
        [SerializeField] private string settingId = "adventurer_guild";

        [Header("Systems")]
        [SerializeField] private TimeManager timeManager;

        [Header("UI")]
        [SerializeField] private DispatchManager           dispatchManager;
        [SerializeField] private CharacterParameterPanelUI paramPanel;
        [SerializeField] private OrgStatusPanelUI          orgStatusPanel;

        // ── Unity ライフサイクル ──────────────────────────────────────
        private void Awake()
        {
            // 最初の1行: 全 JSON 設定を読み込む
            // ここより前に SettingsRegistry.Current を参照してはいけない
            SettingsRegistry.Load(settingId);

            Debug.Log($"[AgentSim] Initialized with setting: '{settingId}' " +
                      $"— {SettingsRegistry.Current.Game.organization_name}");
        }

        private void Start()
        {
            // 今後ここに各マネージャーの初期化を追加する:
            // mapGenerator.Initialize();
            // dispatchManager.Initialize();

            // UI 初期化（dispatchManager が設定されているときのみ）
            if (paramPanel != null && dispatchManager != null)
                paramPanel.Initialize(dispatchManager);
            if (orgStatusPanel != null && dispatchManager != null)
                orgStatusPanel.Initialize(dispatchManager);
        }

        // ── 開発用ユーティリティ ──────────────────────────────────────
#if UNITY_EDITOR
        [ContextMenu("Reload Settings")]
        private void ReloadSettings()
        {
            SettingsRegistry.Load(settingId);
            Debug.Log($"[AgentSim] Settings reloaded: '{settingId}'");
        }

        [ContextMenu("Switch to adventurer_guild")]
        private void SwitchToAdventurerGuild()
        {
            settingId = "adventurer_guild";
            ReloadSettings();
        }

        [ContextMenu("Switch to robot_dispatch")]
        private void SwitchToRobotDispatch()
        {
            settingId = "robot_dispatch";
            ReloadSettings();
        }

        [ContextMenu("Print Config Summary")]
        private void PrintConfigSummary()
        {
            if (SettingsRegistry.Current == null)
            {
                Debug.LogWarning("[AgentSim] SettingsRegistry not loaded yet.");
                return;
            }
            var g = SettingsRegistry.Current.Game;
            var s = SettingsRegistry.Current.Stats;
            var t = SettingsRegistry.Current.Tiers;
            var r = SettingsRegistry.Current.Roles;

            Debug.Log(
                $"[AgentSim] Config Summary — {g.organization_name}\n" +
                $"  Currency  : {g.currency_name} ({g.currency_symbol})\n" +
                $"  Character : {g.characters_term}  Contract: {g.contracts_term}\n" +
                $"  Stats     : {s.primary_stats.Length} primary, {s.derived_stats.Length} derived\n" +
                $"  Tiers     : {t.tiers.Length}  Roles: {r.roles.Length}"
            );
        }
#endif
    }
}



