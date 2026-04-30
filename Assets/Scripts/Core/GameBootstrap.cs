using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

namespace DQSim
{
    [DefaultExecutionOrder(-100)]
    public class GameBootstrap : MonoBehaviour
    {
        [Header("Systems")]
        public TimeManager timeManager;
        public WorldMapGenerator worldMapGenerator;
        public MissionManager missionManager;
        public TilemapRenderer tilemapRenderer;
        public GuildManager guildManager;

        [Header("World")]
        public Grid worldGrid;
        public Tilemap terrainTilemap;
        public Tilemap markerTilemap;

        [Header("UI")]
        public HUDController hudController;
        public GuildScreen guildScreen;
        public Button guildButton;          // "Guild Hall" button on map screen
        public DispatchPanel dispatchPanel;
        public NotificationPanel notificationPanel;

        private void Start()
        {
            // 1. Generate map
            worldMapGenerator.Generate();
            var map = worldMapGenerator.Map;

            // 2. Render tilemap
            tilemapRenderer.RenderMap(map);

            // 3. Initialize systems
            missionManager.Initialize(timeManager, map);
            guildManager.Initialize(map, missionManager, timeManager);

            // 4. Initialize UI
            hudController.Initialize(timeManager, guildManager, missionManager);
            dispatchPanel.Initialize(guildManager);
            guildScreen.Initialize(guildManager, missionManager, timeManager, dispatchPanel);

            // 5. Wire map-screen guild button
            guildButton.onClick.AddListener(() => guildScreen.Open());

            // 6. Mission notifications
            missionManager.OnArrivalAtDestination += m =>
                notificationPanel.Show($"Party arrived: {m.Quest.Title}  |  Returning to base...");
            missionManager.OnReturnedToBase += m =>
                notificationPanel.Show($"Mission complete: {m.Quest.Title}  +{m.Quest.RewardGold}G");

            // 7. Camera
            SetupCamera();
        }

        private void SetupCamera()
        {
            var cam = Camera.main;
            if (cam == null) return;
            cam.orthographic       = true;
            cam.orthographicSize   = 22f;
            cam.transform.position = new Vector3(MapData.Width / 2f, MapData.Height / 2f, -10f);
        }
    }
}
