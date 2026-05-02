using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace DQSim.Battle
{
    public class BattleUnit : MonoBehaviour
    {
        [SerializeField] private float maxMP = 10f;
        [SerializeField] private float currentMP = 10f;
        [SerializeField] private Vector2Int hexPosition;
        [SerializeField] private float moveSpeed = 2.5f;

        public float MaxMP => maxMP;
        public float CurrentMP => currentMP;
        public Vector2Int HexPosition => hexPosition;
        public bool IsMoving { get; private set; }

        private Grid _grid;
        private Tilemap _tilemap;

        public void Initialize(Vector2Int startPos, Grid grid)
        {
            _grid = grid;
            // Search globally if not found on grid object to be safe
            _tilemap = grid.GetComponentInChildren<Tilemap>();
            if (_tilemap == null) _tilemap = Object.FindAnyObjectByType<Tilemap>();
            
            hexPosition = startPos;
            currentMP = maxMP;
            SyncWorldPosition();
        }

        public void ResetMP()
        {
            currentMP = maxMP;
        }

        public void MoveTo(Vector2Int targetPos, List<Vector2Int> path, float cost)
        {
            if (IsMoving) return;
            StartCoroutine(MoveRoutine(targetPos, path, cost));
        }

        private IEnumerator MoveRoutine(Vector2Int targetPos, List<Vector2Int> path, float cost)
        {
            IsMoving = true;
            Vector2Int currentVisualPos = hexPosition;

            // Skip start tile
            for (int i = 1; i < path.Count; i++)
            {
                Vector3 startWorld = GetWorldPos(currentVisualPos);
                Vector3 targetWorld = GetWorldPos(path[i]);
                
                float t = 0;
                while (t < 1f)
                {
                    t += Time.deltaTime * moveSpeed;
                    transform.position = Vector3.Lerp(startWorld, targetWorld, t);
                    yield return null;
                }
                
                currentVisualPos = path[i];
                transform.position = targetWorld;
            }

            hexPosition = targetPos;
            currentMP -= cost;
            if (currentMP < 0) currentMP = 0;
            IsMoving = false;
        }

        private Vector3 GetWorldPos(Vector2Int hexPos)
        {
            var cell = new Vector3Int(hexPos.x, hexPos.y, 0);
            if (_tilemap != null)
                return _tilemap.GetCellCenterWorld(cell);
            if (_grid != null)
                return _grid.GetCellCenterWorld(cell);
            return Vector3.zero;
        }

        public void SyncWorldPosition()
        {
            transform.position = GetWorldPos(hexPosition);
        }
}
}
