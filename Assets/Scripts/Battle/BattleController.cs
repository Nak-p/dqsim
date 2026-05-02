using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Tilemaps;

namespace DQSim.Battle
{
    public class BattleController : MonoBehaviour
{
        public BattleUnit activeUnit;
        public BattleHexMap currentMap;
        public Grid grid;
        public BattleHighlightRenderer highlightRenderer;

        private Dictionary<Vector2Int, BattlePathfinder.ReachableTile> _reachableTiles;
        private Camera _mainCam;

        private void Start()
        {
            _mainCam = Camera.main;
        }

        private void Update()
        {
            if (activeUnit == null) return;

            if (UnityEngine.InputSystem.Mouse.current.leftButton.wasPressedThisFrame)
            {
                if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                    return;

                HandleGridClick();
            }
        }

        public void SetMap(BattleHexMap map)
        {
            currentMap = map;
            RefreshReachable();
        }

        public void RefreshReachable()
        {
            if (activeUnit == null || currentMap == null) return;

            _reachableTiles = BattlePathfinder.GetReachableTiles(currentMap, activeUnit.HexPosition, activeUnit.CurrentMP);
            highlightRenderer.HighlightTiles(_reachableTiles.Keys);
        }

        private void HandleGridClick()
        {
            Vector2 mousePos = UnityEngine.InputSystem.Mouse.current.position.ReadValue();
            
            // Standard 2D camera world coordinate retrieval
            // We use the distance from camera to Z=0 for the depth
            float distance = Mathf.Abs(_mainCam.transform.position.z);
            Vector3 mouseWorld = _mainCam.ScreenToWorldPoint(new Vector3(mousePos.x, mousePos.y, distance));
            mouseWorld.z = 0;
            
            // Accurate world to cell mapping using Tilemap for visual consistency
            var tm = highlightRenderer != null ? highlightRenderer.highlightTilemap : null;
            if (tm == null) tm = grid.GetComponentInChildren<Tilemap>();
            
            Vector3Int cell = tm != null ? tm.WorldToCell(mouseWorld) : grid.WorldToCell(mouseWorld);
            Vector2Int pos = new Vector2Int(cell.x, cell.y);

            if (_reachableTiles != null && _reachableTiles.ContainsKey(pos) && pos != activeUnit.HexPosition)
            {
                var targetNode = _reachableTiles[pos];
                var path = BattlePathfinder.ConstructPath(_reachableTiles, pos);
                if (path != null)
                {
                    activeUnit.MoveTo(pos, path, targetNode.CostToReach);
                    StartCoroutine(WaitAndRefresh());
                }
            }
        }

        private System.Collections.IEnumerator WaitAndRefresh()
        {
            // Clear highlights immediately when movement starts
            highlightRenderer.ClearHighlights();
            
            // Wait until movement finishes
            while (activeUnit != null && activeUnit.IsMoving)
                yield return null;
            
            RefreshReachable();
        }

        public void SpawnUnit()
        {
            if (activeUnit != null) return;

            // Find a walkable tile
            Vector2Int startPos = Vector2Int.zero;
            bool found = false;
            for (int y = 0; y < currentMap.Height; y++)
            {
                for (int x = 0; x < currentMap.Width; x++)
                {
                    if (currentMap.IsWalkable(x, y))
                    {
                        startPos = new Vector2Int(x, y);
                        found = true;
                        break;
                    }
                }
                if (found) break;
            }

            GameObject unitGo = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            unitGo.name = "TestUnit";
            unitGo.transform.SetParent(grid.transform, false);
            unitGo.transform.localScale = Vector3.one * 0.5f;

            // Set sorting order to be above tiles (Terrain: 100, Highlight: 110)
            if (unitGo.TryGetComponent<Renderer>(out var rend))
            {
                rend.sortingLayerName = "Default";
                rend.sortingOrder = 120;
            }

            activeUnit = unitGo.AddComponent<BattleUnit>();
activeUnit.Initialize(startPos, grid);
            
            RefreshReachable();
        }

        public void ResetUnitMP()
        {
            if (activeUnit != null)
            {
                activeUnit.ResetMP();
                RefreshReachable();
            }
        }
    }
}
