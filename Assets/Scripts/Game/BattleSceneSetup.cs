// Assets/Scripts/Game/BattleSceneSetup.cs
// AgentSim — BattleScene 起動スクリプト
//
// [ExecuteAlways] により、Inspector でパラメータを変えると即座にグリッドが再描画される。
// Assets/Scripts/Game/ 配下なので世界観固有コードも許可される。

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using AgentSim.Battle;
using AgentSim.Config;
using AgentSim.Core;
using AgentSim.UI;

namespace AgentSim.Game
{
    public enum BattleGridShape { Hexagonal, Rectangular }

    [ExecuteAlways]
    public class BattleSceneSetup : MonoBehaviour
    {
        // ── Inspector 設定 ────────────────────────────────────────────
        [Header("Setting")]
        [Tooltip("StreamingAssets/settings/ 以下のフォルダ名")]
        [SerializeField] private string settingId = "adventurer_guild";

        [Header("Grid Shape")]
        [Tooltip("Hexagonal: 六角形フィールド / Rectangular: 矩形フィールド")]
        [SerializeField] private BattleGridShape gridShape = BattleGridShape.Rectangular;

        [Tooltip("Hexagonal モード: 0 = game_config.json の battle_grid_radius を使用")]
        [SerializeField, Range(0, 8)] private int gridRadiusOverride = 0;

        [Tooltip("Rectangular モード: グリッドの幅（列数）")]
        [SerializeField, Range(1, 21)] private int gridWidth  = 9;

        [Tooltip("Rectangular モード: グリッドの高さ（行数）")]
        [SerializeField, Range(1, 15)] private int gridHeight = 7;

        [Header("References")]
        [SerializeField] private Tilemap            hexTilemap;
        [SerializeField] private Tilemap            highlightTilemap;
        [SerializeField] private BattleUnitRenderer unitRenderer;
        [SerializeField] private BattleTurnManager  turnManager;
        [SerializeField] private BattleHudUI        hudUI;

        [Header("Test Units")]
        [SerializeField, Range(0, 10)] private int testPartySize  = 3;
        [SerializeField, Range(0, 10)] private int testEnemyCount = 3;
        [SerializeField] private int seed = 42;

        // ── Unity ライフサイクル ──────────────────────────────────────
        private void Start()
        {
            if (Application.isPlaying)
                Rebuild();
        }

        // Inspector 値変更時に自動で再描画（エディタのみ）
#if UNITY_EDITOR
        private void OnValidate()
        {
            UnityEditor.EditorApplication.delayCall += () =>
            {
                if (this != null && hexTilemap != null)
                    Rebuild();
            };
        }
#endif

        // ── 公開 API ─────────────────────────────────────────────────
        [ContextMenu("Rebuild Grid")]
        public void Rebuild()
        {
            if (hexTilemap == null)
            {
                Debug.LogWarning("[BattleSceneSetup] hexTilemap が未設定です。");
                return;
            }

            if (SettingsRegistry.Current == null)
                SettingsRegistry.Load(settingId);

            var        cfg = SettingsRegistry.Current.Game;
            BattleGrid grid;

            if (gridShape == BattleGridShape.Rectangular)
            {
                grid = BattleGrid.CreateRect(gridWidth, gridHeight);
                Debug.Log($"[BattleSceneSetup] Rebuilt — {cfg.organization_name} | " +
                          $"矩形 {gridWidth}×{gridHeight} | " +
                          $"P×{testPartySize} E×{testEnemyCount} seed={seed}");
            }
            else
            {
                int radius = (gridRadiusOverride > 0) ? gridRadiusOverride : cfg.battle_grid_radius;
                grid = new BattleGrid(radius);
                Debug.Log($"[BattleSceneSetup] Rebuilt — {cfg.organization_name} | " +
                          $"六角形 半径 {radius} | " +
                          $"P×{testPartySize} E×{testEnemyCount} seed={seed}");
            }

            BattleTilemapRenderer.Render(grid, hexTilemap);

            if (unitRenderer != null)
            {
                unitRenderer.ClearAll();
                unitRenderer.Initialize(hexTilemap);
            }

            if (unitRenderer == null) return;

            var units = PlaceTestUnits(grid);

            // Play モード時のみターン管理を初期化
            if (Application.isPlaying && turnManager != null && highlightTilemap != null)
            {
                turnManager.Initialize(grid, units, hexTilemap, unitRenderer);
                if (hudUI != null)
                    hudUI.Initialize(turnManager);
            }
        }

        // ── テストユニット配置 ────────────────────────────────────────
        private List<BattleUnit> PlaceTestUnits(BattleGrid grid)
        {
            var rng        = new System.Random(seed);
            var leftHexes  = grid.GetLeftSpawnHexes();
            var rightHexes = grid.GetRightSpawnHexes();

            int playerCount = System.Math.Min(testPartySize,  leftHexes.Count);
            int enemyCount  = System.Math.Min(testEnemyCount, rightHexes.Count);
            var units       = new List<BattleUnit>();

            for (int i = 0; i < playerCount; i++)
            {
                var agent = Agent.Generate(rng);
                var unit  = new BattleUnit(agent, BattleTeam.Player);
                if (grid.PlaceUnit(unit, leftHexes[i]))
                {
                    unitRenderer.PlaceUnit(unit);
                    units.Add(unit);
                }
            }

            for (int i = 0; i < enemyCount; i++)
            {
                var agent = Agent.Generate(rng);
                var unit  = new BattleUnit(agent, BattleTeam.Enemy);
                if (grid.PlaceUnit(unit, rightHexes[i]))
                {
                    unitRenderer.PlaceUnit(unit);
                    units.Add(unit);
                }
            }

            return units;
        }
    }
}


