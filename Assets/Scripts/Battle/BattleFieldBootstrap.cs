using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.Tilemaps;
using DQSim;
// DQSim 名前空間の TilemapRenderer（ワールド地図用）と衝突するため、Unity タイル用を別名にする
using U2D = UnityEngine.Tilemaps;

namespace DQSim.Battle
{
    /// <summary>バトルテストシーン用: Hex Grid + Tilemap を用意し、フィールドを生成して描画する。</summary>
    /// <remarks>
    /// GameBootstrap は DefaultExecutionOrder(-100) で Start が先に走ると四角ワールドが描かれるため、
    /// このコンポーネントより早く Awake でワールド用オブジェクトを無効化する。
    /// </remarks>
    [DefaultExecutionOrder(-300)]
    public class BattleFieldBootstrap : MonoBehaviour
    {
        [Header("Field")]
        [SerializeField] private int width = BattleFieldGenerator.DefaultWidth;
        [SerializeField] private int height = BattleFieldGenerator.DefaultHeight;
        [SerializeField] private int seed = 42;

        [Tooltip("未割り当てなら子に Hex Grid と Terrain Tilemap を生成する")]
        [SerializeField] private bool createGridIfMissing = true;

        [Tooltip("SampleScene マージ時など、GameBootstrap とルート World をオフにしてヘックスだけ表示する")]
        [SerializeField] private bool suppressWorldMapHierarchy = true;

        [Tooltip("ギルド UI を名前で個別にオフ（ルート UI ごとは無効にしない — その配下に BattleField があると真っ暗になるため）")]
        [FormerlySerializedAs("suppressUiRoot")]
        [SerializeField] private bool suppressGuildUiPanels = true;

        [Tooltip("Screen Space Overlay の Canvas がマップの手前に残ることがあるため、BattleField を含まない Overlay を無効化する")]
        [SerializeField] private bool suppressScreenSpaceOverlayCanvases = true;

        [Tooltip("オフにすると四角 Grid（検証用）。オンのまま＝六角 Grid")]
        [SerializeField] private bool hexagonalGrid = true;

        private BattleHexTilemapRenderer _hexRenderer;
        private Grid _grid;

        private void Awake()
        {
            if (suppressWorldMapHierarchy || suppressGuildUiPanels || suppressScreenSpaceOverlayCanvases)
                SuppressMergedSceneObjects();

            _hexRenderer = GetComponent<BattleHexTilemapRenderer>();
            if (_hexRenderer == null)
                _hexRenderer = gameObject.AddComponent<BattleHexTilemapRenderer>();

            if (_hexRenderer.terrainTilemap == null && createGridIfMissing)
                CreateHexGridHierarchy();
        }

        /// <summary>マージされたワールド／ギルド UI を無効化（GameBootstrap.Start より前に実行）。</summary>
        private void SuppressMergedSceneObjects()
        {
            if (suppressWorldMapHierarchy)
            {
                foreach (var gb in FindObjectsByType<GameBootstrap>(FindObjectsInactive.Include))
                    gb.gameObject.SetActive(false);
            }

            var scene = gameObject.scene;
            if (!scene.IsValid()) return;

            foreach (var root in scene.GetRootGameObjects())
            {
                if (suppressWorldMapHierarchy && root.name == "World")
                    root.SetActive(false);
            }

            if (suppressGuildUiPanels)
                SuppressGuildUiPanelsByName(scene);

            if (suppressScreenSpaceOverlayCanvases)
                SuppressScreenSpaceOverlayCanvases();
        }

        /// <summary>ギルドなど BattleField を含まない Screen Space Overlay を無効化（全面マスク対策）。</summary>
        private void SuppressScreenSpaceOverlayCanvases()
        {
            foreach (var canvas in FindObjectsByType<Canvas>(FindObjectsInactive.Include))
            {
                if (canvas.renderMode != RenderMode.ScreenSpaceOverlay) continue;
                // 子にだけ Bootstrap があってもダメ。BattleUI の Canvas は BattleField の子なので祖先を見る。
                if (canvas.GetComponentInParent<BattleFieldBootstrap>(true) != null) continue;
                canvas.gameObject.SetActive(false);
            }
        }

        /// <summary>名前一致するギルド UI のみ非表示。BattleField の祖先は触らない。</summary>
        private void SuppressGuildUiPanelsByName(UnityEngine.SceneManagement.Scene scene)
        {
            var panelNames = new[]
            {
                "DispatchPanel",
                "GuildScreen",
                "HUD",
                "GuildHallBtn",
                "NotificationPanel",
            };

            var battleTf = transform;

            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var tr in root.GetComponentsInChildren<Transform>(true))
                {
                    if (tr == battleTf) continue;
                    if (battleTf.IsChildOf(tr)) continue;

                    foreach (var nm in panelNames)
                    {
                        if (tr.gameObject.name != nm) continue;
                        tr.gameObject.SetActive(false);
                        break;
                    }
                }
            }
        }

        private void Start()
        {
            if (_grid == null)
                _grid = GetComponentInChildren<Grid>(true);

            var map = BattleFieldGenerator.Generate(seed, width, height);
            _hexRenderer.RenderMap(map);

            var controller = GetComponent<BattleController>();
            if (controller != null)
                controller.SetMap(map);

            PositionCamera(map);
#if UNITY_EDITOR
            var cam = Camera.main ?? FindAnyObjectByType<Camera>();
            string layoutStr = _grid != null ? _grid.cellLayout.ToString() : "?";
            string posStr = cam != null ? cam.transform.position.ToString() : "null";
            string sizeStr = cam != null ? cam.orthographicSize.ToString() : "";
            Debug.Log(
                $"BattleFieldBootstrap: grid={(_grid != null)} layout={layoutStr} camPos={posStr} orthoSize={sizeStr}");
            ValidateNeighborsSample(map);
            VerifySeedReproducibility();
            VerifyAllCellsInBounds(map);
#endif
        }

        /// <summary>
        /// Unity の Hexagonal Point Top と同一セル座標 (x=列,y=行)。セルサイズはエディタ「Hexagonal Point Top」と同系統。
        /// </summary>
        private void CreateHexGridHierarchy()
        {
            Transform existingGrid = transform.Find("BattleHexGrid");
            GameObject root;
            if (existingGrid != null)
            {
                root = existingGrid.gameObject;
            }
            else
            {
                root = new GameObject("BattleHexGrid");
                root.transform.SetParent(transform, false);
            }

            _grid = root.GetComponent<Grid>();
            if (_grid == null)
                _grid = root.AddComponent<Grid>();

            // FORCE correct hexagonal settings
            if (hexagonalGrid)
            {
                _grid.cellLayout = GridLayout.CellLayout.Hexagon;
                _grid.cellSize = new Vector3(0.8660254f, 1f, 1f);
                _grid.cellSwizzle = GridLayout.CellSwizzle.XYZ;
            }
            else
            {
                _grid.cellLayout = GridLayout.CellLayout.Rectangle;
                _grid.cellSize = new Vector3(1f, 1f, 1f);
            }

            Transform terrainTr = root.transform.Find("Terrain");
            GameObject tmGo;
            if (terrainTr != null)
            {
                tmGo = terrainTr.gameObject;
            }
            else
            {
                tmGo = new GameObject("Terrain");
                tmGo.transform.SetParent(root.transform, false);
            }

            var tm = tmGo.GetComponent<Tilemap>();
            if (tm == null) tm = tmGo.AddComponent<Tilemap>();
            
            // Standard Hexagon alignment: anchor is 0.5, 0.5 for centering sprites with 0.5 pivot
            tm.tileAnchor = new Vector3(0.5f, 0.5f, 0f);
            tm.color = Color.white;

            var tmr = tmGo.GetComponent<U2D.TilemapRenderer>();
            if (tmr == null) tmr = tmGo.AddComponent<U2D.TilemapRenderer>();

            if (tmr.TryGetComponent<Renderer>(out var rendSort))
                rendSort.sortingLayerID = SortingLayer.NameToID("Default");
            
            BattleHexTilemapRenderer.ApplyUnlitMaterialForUrp2D(tmr);
            
            if (tmGo.TryGetComponent<Renderer>(out var tileRend))
                tileRend.sortingOrder = 100;

            _hexRenderer.terrainTilemap = tm;
        }

        private void PositionCamera(BattleHexMap map)
        {
            var cam = FindPreferredCamera();
            if (cam == null) return;

            if (_grid == null)
                _grid = GetComponentInChildren<Grid>(true);
            if (_grid == null)
                _grid = FindAnyObjectByType<Grid>();

            cam.orthographic = true;

            // シンプルに全セル原点の包み込み（GetCellCenterWorld や aspect 式は環境差で外れやすい）
            if (_grid != null)
            {
                float minX = float.MaxValue, minY = float.MaxValue;
                float maxX = float.MinValue, maxY = float.MinValue;
                for (int x = 0; x < map.Width; x++)
                {
                    for (int y = 0; y < map.Height; y++)
                    {
                        var w = _grid.CellToWorld(new Vector3Int(x, y, 0));
                        minX = Mathf.Min(minX, w.x);
                        minY = Mathf.Min(minY, w.y);
                        maxX = Mathf.Max(maxX, w.x);
                        maxY = Mathf.Max(maxY, w.y);
                    }
                }

                Vector3 cs = _grid.cellSize;
                minX -= Mathf.Abs(cs.x) * 0.6f;
                maxX += Mathf.Abs(cs.x) * 0.6f;
                minY -= Mathf.Abs(cs.y) * 0.6f;
                maxY += Mathf.Abs(cs.y) * 0.6f;

                var center = new Vector3((minX + maxX) * 0.5f, (minY + maxY) * 0.5f, -10f);
                cam.transform.position = center;
                // orthographicSize は「半分の高さ」。横方向は aspect で狭まるため両方を満たす必要がある
                float aspect = cam.aspect > 0.001f ? cam.aspect : 16f / 9f;
                float spanX = maxX - minX;
                float spanY = maxY - minY;
                float orthoForHeight = spanY * 0.5f;
                float orthoForWidth = spanX / (2f * aspect);
                float orthoBase = Mathf.Max(orthoForHeight, orthoForWidth);
                cam.orthographicSize = Mathf.Max(orthoBase * 1.2f + 0.6f, 6f);
                return;
            }

            cam.transform.position = new Vector3(width * 0.5f, height * 0.5f, -10f);
            cam.orthographicSize = Mathf.Max(height * 0.55f, 8f);
        }

        /// <summary>MainCamera を優先。UI 専用カメラを拾わないようタグ付きを先に探す。</summary>
        private static Camera FindPreferredCamera()
        {
            var cam = Camera.main;
            if (cam != null && cam.isActiveAndEnabled)
                return cam;

            foreach (var c in FindObjectsByType<Camera>(FindObjectsInactive.Exclude))
            {
                if (!c.isActiveAndEnabled) continue;
                if (c.CompareTag("MainCamera"))
                    return c;
            }

            return FindAnyObjectByType<Camera>(FindObjectsInactive.Exclude);
        }

#if UNITY_EDITOR
        /// <summary>内部セルで隣接が 6 件になることを確認（odd-r / Point Top）。</summary>
        private static void ValidateNeighborsSample(BattleHexMap map)
        {
            var buf = new System.Collections.Generic.List<Vector2Int>(6);
            int cx = map.Width / 2;
            int cy = map.Height / 2;
            if (cx < 1 || cy < 1 || cx >= map.Width - 1 || cy >= map.Height - 1)
                return;

            HexCoordinates.GetNeighbors(map, cx, cy, buf);
            if (buf.Count != 6)
                Debug.LogWarning($"BattleFieldBootstrap: expected 6 neighbors at ({cx},{cy}), got {buf.Count}");
        }

        private void VerifySeedReproducibility()
        {
            var a = BattleFieldGenerator.Generate(seed, width, height);
            var b = BattleFieldGenerator.Generate(seed, width, height);
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    if (a.Get(x, y) != b.Get(x, y))
                    {
                        Debug.LogError($"BattleFieldBootstrap: seed {seed} not reproducible at ({x},{y})");
                        return;
                    }
                }
            }
        }

        private static void VerifyAllCellsInBounds(BattleHexMap map)
        {
            for (int x = 0; x < map.Width; x++)
            {
                for (int y = 0; y < map.Height; y++)
                {
                    if (!map.InBounds(x, y))
                    {
                        Debug.LogError($"BattleFieldBootstrap: InBounds false for ({x},{y})");
                        return;
                    }
                }
            }
        }
#endif
    }
}
