// Assets/Scripts/Game/BattleSceneSetup.cs
// AgentSim — BattleScene 起動スクリプト
//
// Assets/Scripts/Game/ 配下なので世界観固有コードも許可される。
// BattleScene の Bootstrap GameObject にアタッチする。
//
// Unity Inspector での設定:
//   settingId   : "adventurer_guild" または "robot_dispatch"
//   hexTilemap  : Grid > Tilemap コンポーネントを参照
//   unitRenderer: BattleUnitRenderer コンポーネントを参照

using UnityEngine;
using UnityEngine.Tilemaps;
using AgentSim.Battle;
using AgentSim.Config;
using AgentSim.Core;

namespace AgentSim.Game
{
    public class BattleSceneSetup : MonoBehaviour
    {
        // ── Inspector 設定 ────────────────────────────────────────────
        [Header("Setting")]
        [Tooltip("StreamingAssets/settings/ 以下のフォルダ名")]
        [SerializeField] private string settingId = "adventurer_guild";

        [Header("References")]
        [SerializeField] private Tilemap           hexTilemap;
        [SerializeField] private BattleUnitRenderer unitRenderer;

        [Header("Test Parameters")]
        [SerializeField] private int testPartySize  = 3;
        [SerializeField] private int testEnemyCount = 3;
        [SerializeField] private int seed           = 42;

        // ── Unity ライフサイクル ──────────────────────────────────────
        private void Awake()
        {
            SettingsRegistry.Load(settingId);
        }

        private void Start()
        {
            if (hexTilemap == null)
            {
                Debug.LogError("[BattleSceneSetup] hexTilemap が設定されていません。Inspector で参照を設定してください。");
                return;
            }

            var cfg  = SettingsRegistry.Current.Game;
            var grid = new BattleGrid(cfg.battle_grid_radius);

            // グリッドを Tilemap に描画
            BattleTilemapRenderer.Render(grid, hexTilemap);

            // ユニットレンダラーを初期化
            if (unitRenderer != null)
            {
                unitRenderer.Initialize(hexTilemap);
                PlaceTestUnits(grid);
            }

            Debug.Log($"[BattleSceneSetup] BattleScene 初期化完了: {cfg.organization_name} " +
                      $"| グリッド半径 {cfg.battle_grid_radius}");
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
                var agent = Agent.Generate(rng);
                var unit  = new BattleUnit(agent, BattleTeam.Player);
                if (grid.PlaceUnit(unit, leftHexes[i]))
                    unitRenderer.PlaceUnit(unit);
            }

            for (int i = 0; i < enemyCount; i++)
            {
                var agent = Agent.Generate(rng);
                var unit  = new BattleUnit(agent, BattleTeam.Enemy);
                if (grid.PlaceUnit(unit, rightHexes[i]))
                    unitRenderer.PlaceUnit(unit);
            }

            Debug.Log($"[BattleSceneSetup] テストユニット配置: プレイヤー {playerCount}体 / 敵 {enemyCount}体");
        }
    }
}
