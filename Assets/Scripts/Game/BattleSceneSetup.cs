// Assets/Scripts/Game/BattleSceneSetup.cs
// AgentSim — BattleScene 起動スクリプト
//
// [ExecuteAlways] により、Inspector でパラメータを変えると即座にグリッドが再描画される。
// Assets/Scripts/Game/ 配下なので世界観固有コードも許可される。

using UnityEngine;
using UnityEngine.Tilemaps;
using AgentSim.Battle;
using AgentSim.Config;
using AgentSim.Core;

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
        [SerializeField] private BattleUnitRenderer unitRenderer;

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
            // delayCall を使わないと OnValidate 内から Rebuild が呼べない
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

            // SettingsRegistry をロード（エディタ実行中でも動作）
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

            // グリッドを Tilemap に描画
            BattleTilemapRenderer.Render(grid, hexTilemap);

            // ユニットをリセットして再配置
            if (unitRenderer != null)
            {
                unitRenderer.ClearAll();
                unitRenderer.Initialize(hexTilemap);
                PlaceTestUnits(grid);
            }
        }

        // ── テストユニット配置 ────────────────────────────────────────
        private void PlaceTestUnits(BattleGrid grid)
        {
            var rng        = new System.Random(seed);
            var leftHexes  = grid.GetLeftSpawnHexes();
            var rightHexes = grid.GetRightSpawnHexes();

            int playerCount = System.Math.Min(testPartySize,  leftHexes.Count);
            int enemyCount  = System.Math.Min(testEnemyCount, rightHexes.Count);

            for (int i = 0; i < playerCount; i++)
            {
                var character = Character.Generate(rng);
                var unit  = new BattleUnit(character, BattleTeam.Player);
                if (grid.PlaceUnit(unit, leftHexes[i]))
                    unitRenderer.PlaceUnit(unit);
            }

            for (int i = 0; i < enemyCount; i++)
            {
                var character = Character.Generate(rng);
                var unit  = new BattleUnit(character, BattleTeam.Enemy);
                if (grid.PlaceUnit(unit, rightHexes[i]))
                    unitRenderer.PlaceUnit(unit);
            }
        }
    }
}
