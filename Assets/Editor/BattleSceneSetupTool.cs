// Assets/Editor/BattleSceneSetupTool.cs
// AgentSim — バトルシーンの HighlightTilemap を自動セットアップするエディタツール
//
// メニュー: AgentSim > Setup Battle HighlightTilemap
// 実行すると:
//   1. Grid/HighlightTilemap に Tilemap + TilemapRenderer を追加（なければ）
//   2. BattleSceneSetup.highlightTilemap を HighlightTilemap に差し替え
//   3. シーンを dirty にする（Ctrl+S で保存可能）

using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;
using AgentSim.Game;

public static class BattleSceneSetupTool
{
    [MenuItem("AgentSim/Setup Battle HighlightTilemap")]
    static void SetupHighlightTilemap()
    {
        // ── 1. Grid/HighlightTilemap を検索 ──────────────────────────
        // "HighlightTilemap" という名前の GameObject を全シーンから探す
        var allObjects = Object.FindObjectsByType<Transform>(FindObjectsSortMode.None);
        GameObject highlightGO = null;

        foreach (var t in allObjects)
        {
            if (t.name == "HighlightTilemap" && t.parent != null && t.parent.name == "Grid")
            {
                highlightGO = t.gameObject;
                break;
            }
        }

        if (highlightGO == null)
        {
            // Grid 直下でなくても名前が "HighlightTilemap" なら候補にする
            foreach (var t in allObjects)
            {
                if (t.name == "HighlightTilemap")
                {
                    highlightGO = t.gameObject;
                    break;
                }
            }
        }

        if (highlightGO == null)
        {
            EditorUtility.DisplayDialog("Setup Failed",
                "HighlightTilemap GameObject が見つかりません。\n" +
                "Hierarchy に Grid > HighlightTilemap が存在するか確認してください。", "OK");
            return;
        }

        // ── 2. Tilemap コンポーネントを追加 ──────────────────────────
        var tilemap = highlightGO.GetComponent<Tilemap>();
        if (tilemap == null)
        {
            tilemap = highlightGO.AddComponent<Tilemap>();
            Debug.Log("[BattleSetupTool] Tilemap を追加しました。");
        }

        // ── 3. TilemapRenderer コンポーネントを追加・設定 ────────────
        var renderer = highlightGO.GetComponent<TilemapRenderer>();
        if (renderer == null)
        {
            renderer = highlightGO.AddComponent<TilemapRenderer>();
            Debug.Log("[BattleSetupTool] TilemapRenderer を追加しました。");
        }
        renderer.sortingOrder = 110;

        // ── 4. BattleSceneSetup.highlightTilemap を差し替え ──────────
        var setup = Object.FindFirstObjectByType<BattleSceneSetup>();
        if (setup != null)
        {
            var so   = new SerializedObject(setup);
            var prop = so.FindProperty("highlightTilemap");
            if (prop != null)
            {
                prop.objectReferenceValue = tilemap;
                so.ApplyModifiedProperties();
                Debug.Log("[BattleSetupTool] BattleSceneSetup.highlightTilemap を " +
                          highlightGO.name + " に更新しました。");
            }
            else
            {
                Debug.LogWarning("[BattleSetupTool] BattleSceneSetup に highlightTilemap フィールドが見つかりません。");
            }
        }
        else
        {
            Debug.LogWarning("[BattleSetupTool] BattleSceneSetup が見つかりません。手動でアサインしてください。");
        }

        // ── 5. シーンを dirty にして保存促進 ─────────────────────────
        EditorUtility.SetDirty(highlightGO);
        if (setup != null) EditorUtility.SetDirty(setup);
        EditorSceneManager.MarkSceneDirty(highlightGO.scene);

        EditorUtility.DisplayDialog("Setup Complete",
            $"HighlightTilemap のセットアップが完了しました。\n\n" +
            $"  GameObject : {highlightGO.name}\n" +
            $"  SortingOrder : {renderer.sortingOrder}\n\n" +
            "Ctrl+S でシーンを保存してください。", "OK");
    }
}
