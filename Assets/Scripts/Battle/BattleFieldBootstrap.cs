using UnityEngine;
using UnityEngine.Tilemaps;

namespace DQSim.Battle
{
    /// <summary>バトルテストシーン用: Hex Grid + Tilemap を用意し、フィールドを生成して描画する。</summary>
    [DefaultExecutionOrder(-50)]
    public class BattleFieldBootstrap : MonoBehaviour
    {
        [Header("Field")]
        [SerializeField] private int width = BattleFieldGenerator.DefaultWidth;
        [SerializeField] private int height = BattleFieldGenerator.DefaultHeight;
        [SerializeField] private int seed = 42;

        [Tooltip("未割り当てなら子に Hex Grid と Terrain Tilemap を生成する")]
        [SerializeField] private bool createGridIfMissing = true;

        private BattleHexTilemapRenderer _hexRenderer;
        private Grid _grid;

        private void Awake()
        {
            _hexRenderer = GetComponent<BattleHexTilemapRenderer>();
            if (_hexRenderer == null)
                _hexRenderer = gameObject.AddComponent<BattleHexTilemapRenderer>();

            if (_hexRenderer.terrainTilemap == null && createGridIfMissing)
                CreateHexGridHierarchy();
        }

        private void Start()
        {
            var map = BattleFieldGenerator.Generate(seed, width, height);
            _hexRenderer.RenderMap(map);
            PositionCamera(map);
#if UNITY_EDITOR
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
            var root = new GameObject("BattleHexGrid");
            root.transform.SetParent(transform, false);

            _grid = root.AddComponent<Grid>();
            _grid.cellLayout = GridLayout.CellLayout.Hexagon;
            _grid.cellSize = new Vector3(0.8660254f, 1f, 1f);
            _grid.cellGap = Vector3.zero;

            var tmGo = new GameObject("Terrain");
            tmGo.transform.SetParent(root.transform, false);
            var tm = tmGo.AddComponent<Tilemap>();
            tmGo.AddComponent<TilemapRenderer>();

            _hexRenderer.terrainTilemap = tm;
        }

        private void PositionCamera(BattleHexMap map)
        {
            var cam = Camera.main;
            if (cam == null) return;

            if (_grid == null)
                _grid = FindFirstObjectByType<Grid>();

            cam.orthographic = true;

            if (_grid != null)
            {
                var corners = new[]
                {
                    _grid.CellToWorld(new Vector3Int(0, 0, 0)),
                    _grid.CellToWorld(new Vector3Int(map.Width - 1, 0, 0)),
                    _grid.CellToWorld(new Vector3Int(0, map.Height - 1, 0)),
                    _grid.CellToWorld(new Vector3Int(map.Width - 1, map.Height - 1, 0)),
                };

                Vector3 sum = Vector3.zero;
                foreach (var c in corners) sum += c;
                Vector3 center = sum / corners.Length;
                center.z = cam.transform.position.z;

                float maxDist = 0f;
                foreach (var c in corners)
                {
                    float d = Mathf.Max(Mathf.Abs(c.x - center.x), Mathf.Abs(c.y - center.y));
                    if (d > maxDist) maxDist = d;
                }

                cam.transform.position = center;
                cam.orthographicSize = Mathf.Max(maxDist * 1.15f + 0.5f, 5f);
            }
            else
            {
                cam.transform.position = new Vector3(width * 0.5f, height * 0.5f, cam.transform.position.z);
                cam.orthographicSize = Mathf.Max(height * 0.55f, 8f);
            }
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
