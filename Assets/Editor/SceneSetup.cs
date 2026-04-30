using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEngine.Tilemaps;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using TMPro;
using DQSim;

public class SceneSetup : MonoBehaviour
{
    [MenuItem("DQSim/Setup Scene")]
    public static void SetupScene()
    {
        // ── Cleanup ───────────────────────────────────────────────────────
        foreach (var n in new[] { "Systems", "World", "UI", "EventSystem" })
            DestroyImmediate(GameObject.Find(n));
        foreach (var sim in Object.FindObjectsOfType<StandaloneInputModule>(true))
            DestroyImmediate(sim.gameObject);
        foreach (var es in Object.FindObjectsOfType<EventSystem>(true))
            DestroyImmediate(es.gameObject);

        // ── EventSystem ───────────────────────────────────────────────────
        var esGO = new GameObject("EventSystem");
        esGO.AddComponent<EventSystem>();
        esGO.AddComponent<InputSystemUIInputModule>();

        // ── Systems ───────────────────────────────────────────────────────
        var systemsGO   = new GameObject("Systems");
        var bootstrapGO = Create<GameBootstrap>("GameBootstrap",         systemsGO.transform);
        var timeMgr     = Create<TimeManager>("TimeManager",             systemsGO.transform);
        var mapGen      = Create<WorldMapGenerator>("WorldMapGenerator", systemsGO.transform);
        var missionMgr  = Create<MissionManager>("MissionManager",       systemsGO.transform);
        var tileRend    = Create<DQSim.TilemapRenderer>("TilemapRenderer", systemsGO.transform);
        var guildMgr    = Create<GuildManager>("GuildManager",           systemsGO.transform);

        // ── World ─────────────────────────────────────────────────────────
        var worldGO = new GameObject("World");
        var gridGO  = new GameObject("Grid");
        gridGO.transform.SetParent(worldGO.transform);
        gridGO.AddComponent<Grid>().cellSize = Vector3.one;

        var terrainGO = MakeTilemap("TerrainTilemap", gridGO.transform, 0);
        var markerGO  = MakeTilemap("MarkerTilemap",  gridGO.transform, 5);
        tileRend.terrainTilemap = terrainGO.GetComponent<Tilemap>();
        tileRend.markerTilemap  = markerGO.GetComponent<Tilemap>();

        // ── Canvas ────────────────────────────────────────────────────────
        var canvasGO = new GameObject("UI");
        var canvas   = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvasGO.AddComponent<GraphicRaycaster>();

        // ── MAP SCREEN ────────────────────────────────────────────────────
        // HUD — top-left 300x110 (Awake self-builds TMP labels)
        var hudGO   = FixedPanel("HUD", canvasGO.transform, new Color(0, 0, 0, 0.65f),
                        new Vector2(0, 1), new Vector2(155, -60), new Vector2(300, 110));
        var hudComp = hudGO.AddComponent<HUDController>();

        // Guild Hall button — just below HUD
        var guildBtnGO = FixedPanel("GuildHallBtn", canvasGO.transform,
                            new Color(0.55f, 0.40f, 0.04f, 0.92f),
                            new Vector2(0, 1), new Vector2(155, -143), new Vector2(300, 36));
        var guildBtn = guildBtnGO.AddComponent<Button>();
        UIBuilder.AddLabel(guildBtnGO.transform, "[ Guild Hall ]", 15,
            Color.white, TextAlignmentOptions.Center);

        // ── GUILD HALL SCREEN (full-screen overlay, Awake will hide it) ───
        var gsGO = new GameObject("GuildScreen");
        gsGO.transform.SetParent(canvasGO.transform, false);
        gsGO.AddComponent<Image>();           // color set by GuildScreen.Awake()
        var gsRT = gsGO.GetComponent<RectTransform>();
        gsRT.anchorMin = Vector2.zero;
        gsRT.anchorMax = Vector2.one;
        gsRT.offsetMin = Vector2.zero;
        gsRT.offsetMax = Vector2.zero;
        var gsComp = gsGO.AddComponent<GuildScreen>();

        // ── DISPATCH MODAL (over GuildScreen, Awake will hide it) ─────────
        var dpGO     = FixedPanel("DispatchPanel", canvasGO.transform,
                            new Color(0.05f, 0.05f, 0.2f, 0.97f),
                            new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(540, 580));
        var dispComp = dpGO.AddComponent<DispatchPanel>();

        // ── NOTIFICATION BANNER (always on top) ───────────────────────────
        var notifGO   = FixedPanel("NotificationPanel", canvasGO.transform,
                            new Color(0.1f, 0.55f, 0.2f, 0.9f),
                            new Vector2(0.5f, 1), new Vector2(0, -28), new Vector2(640, 48));
        var notifComp = notifGO.AddComponent<NotificationPanel>();

        // ── Wire GameBootstrap ────────────────────────────────────────────
        var bs = bootstrapGO.GetComponent<GameBootstrap>();
        bs.timeManager       = timeMgr;
        bs.worldMapGenerator = mapGen;
        bs.missionManager    = missionMgr;
        bs.tilemapRenderer   = tileRend;
        bs.guildManager      = guildMgr;
        bs.worldGrid         = gridGO.GetComponent<Grid>();
        bs.terrainTilemap    = tileRend.terrainTilemap;
        bs.markerTilemap     = tileRend.markerTilemap;
        bs.hudController     = hudComp;
        bs.guildScreen       = gsComp;
        bs.guildButton       = guildBtn;
        bs.dispatchPanel     = dispComp;
        bs.notificationPanel = notifComp;

        Debug.Log("DQSim: Scene setup complete! Press Play to test.");
        EditorUtility.SetDirty(bootstrapGO);
    }

    // ─── Helpers ──────────────────────────────────────────────────────────

    private static T Create<T>(string name, Transform parent) where T : Component
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent);
        return go.AddComponent<T>();
    }

    private static GameObject MakeTilemap(string name, Transform parent, int sortOrder)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent);
        go.AddComponent<Tilemap>();
        go.AddComponent<UnityEngine.Tilemaps.TilemapRenderer>().sortingOrder = sortOrder;
        return go;
    }

    /// <summary>Single-point anchor panel with fixed size.</summary>
    private static GameObject FixedPanel(string name, Transform parent, Color bg,
        Vector2 anchor, Vector2 anchoredPos, Vector2 size)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<Image>().color = bg;
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin        = anchor;
        rt.anchorMax        = anchor;
        rt.pivot            = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta        = size;
        return go;
    }
}
