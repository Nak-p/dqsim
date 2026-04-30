using System;
using System.Collections.Generic;
using UnityEngine;

namespace DQSim
{
    public class PartyController : MonoBehaviour
    {
        public float partySpeed = 2f;

        public event Action OnArrivedAtDestination;
        public event Action OnArrivedAtBase;

        private TimeManager _timeManager;
        private List<Vector2Int> _path;
        private int _pathIndex;
        private float _progress;
        private bool _isMoving;
        private bool _goingToDestination;
        private SpriteRenderer _spriteRenderer;

        private void Awake()
        {
            _spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
            _spriteRenderer.sprite = CreatePartySprite(new Color32(0xf0, 0xd0, 0x60, 0xff));
            _spriteRenderer.sortingOrder = 10;
        }

        public void Initialize(TimeManager timeManager, Color32 partyColor = default)
        {
            _timeManager = timeManager;
            if (partyColor.a > 0)
                _spriteRenderer.sprite = CreatePartySprite(partyColor);
        }

        public void StartMoving(List<Vector2Int> path, bool toDestination)
        {
            if (path == null || path.Count < 2) return;
            _path             = path;
            _pathIndex        = 1;
            _progress         = 0f;
            _isMoving         = true;
            _goingToDestination = toDestination;
            transform.position = TileToWorld(path[0]);
        }

        public void PlaceAt(Vector2Int tile)
        {
            transform.position = TileToWorld(tile);
            _isMoving = false;
            _path     = null;
        }

        private void Update()
        {
            if (!_isMoving || _timeManager == null || _timeManager.IsPaused) return;

            float gameSecondsPerRealSecond = 86400f / _timeManager.realSecondsPerGameDay;
            float inGameHoursThisFrame     = Time.deltaTime * gameSecondsPerRealSecond / 3600f;
            float tilesThisFrame           = partySpeed * inGameHoursThisFrame;

            _progress += tilesThisFrame;

            while (_progress >= 1f && _pathIndex < _path.Count - 1)
            {
                _progress -= 1f;
                _pathIndex++;
            }

            if (_pathIndex >= _path.Count - 1 && _progress >= 1f)
            {
                transform.position = TileToWorld(_path[_path.Count - 1]);
                _isMoving = false;
                _progress = 0f;

                if (_goingToDestination) OnArrivedAtDestination?.Invoke();
                else                     OnArrivedAtBase?.Invoke();
                return;
            }

            transform.position = Vector3.Lerp(
                TileToWorld(_path[_pathIndex - 1]),
                TileToWorld(_path[_pathIndex]),
                _progress);
        }

        private Vector3 TileToWorld(Vector2Int tile) =>
            new Vector3(tile.x + 0.5f, tile.y + 0.5f, -1f);

        private Sprite CreatePartySprite(Color32 body)
        {
            int size = 12;
            var tex = new Texture2D(size, size) { filterMode = FilterMode.Point };
            var pixels = new Color32[size * size];
            Color32 bg   = new Color32(0, 0, 0, 0);
            Color32 dark = new Color32((byte)(body.r / 2), (byte)(body.g / 2), (byte)(body.b / 2), 0xff);

            for (int i = 0; i < pixels.Length; i++) pixels[i] = bg;
            SetRect(pixels, size, 4, 9, 7, 11, body);
            SetRect(pixels, size, 3, 5, 8,  8, body);
            SetRect(pixels, size, 1, 5, 2,  7, body);
            SetRect(pixels, size, 9, 5, 10, 7, body);
            SetRect(pixels, size, 3, 2, 5,  4, body);
            SetRect(pixels, size, 6, 2, 8,  4, body);
            SetRect(pixels, size, 4, 9, 7,  9, dark);

            tex.SetPixels32(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        }

        private void SetRect(Color32[] pixels, int size, int x0, int y0, int x1, int y1, Color32 color)
        {
            for (int y = y0; y <= y1; y++)
                for (int x = x0; x <= x1; x++)
                    if (x >= 0 && x < size && y >= 0 && y < size)
                        pixels[y * size + x] = color;
        }
    }
}
