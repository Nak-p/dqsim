// Assets/Scripts/Game/MapTest.cs
// AgentSim — マップ生成 + 派遣動作確認用スクリプト（Game/ 層）

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using AgentSim.Config;
using AgentSim.Core;
using AgentSim.Map;
using AgentSim.Systems;

public class MapTest : MonoBehaviour
{
    [Header("Map")]
    public WorldMapGenerator mapGenerator;
    public TilemapRenderer   tilemapRenderer;
    public int               seed = 42;

    [Header("Dispatch")]
    public DispatchManager dispatchManager;
    public TimeManager     timeManager;

    private void Start()
    {
        // マップ生成・描画
        mapGenerator.Generate(seed);
        if (mapGenerator.Map == null) return;

        tilemapRenderer.RenderMap(mapGenerator.Map);

        // DispatchManager 初期化
        dispatchManager.Initialize(mapGenerator.Map, timeManager, seed);

        // ミッション完了時のログ
        dispatchManager.OnMissionComplete += m =>
        {
            string sym = SettingsRegistry.Current?.Game?.currency_symbol ?? "";
            Debug.Log($"[Test] ミッション完了: {m.Contract.Title} | 組織残高: {dispatchManager.OrgCurrency}{sym}");
        };

        // 派遣開始時のログ
        dispatchManager.OnMissionDispatched += m =>
            Debug.Log($"[Test] 派遣開始: {m.Contract.Title} → {m.Contract.Destination}");
    }

    private void Update()
    {
        // スペースキー: 最初の案件に最初のエージェントを派遣
        if (Keyboard.current.spaceKey.wasPressedThisFrame)
            TryDispatchFirst();
    }

    private void TryDispatchFirst()
    {
        // 条件を満たせる (案件, エージェント) の組み合わせを自動検索
        foreach (var contract in dispatchManager.AvailableContracts)
        {
            foreach (var agent in dispatchManager.Roster)
            {
                if (!agent.IsAvailable) continue;
                var party = new List<Agent> { agent };
                if (!dispatchManager.CanDispatch(contract, party)) continue;

                dispatchManager.Dispatch(contract, party);
                Debug.Log($"[Test] 派遣: {agent.Name}({agent.TierId}) → {contract.Title}({contract.MinTierId})");
                return;
            }
        }
        Debug.Log("[Test] 派遣可能な組み合わせが見つかりません（全員出動中 or ティア不足）");
    }
}
