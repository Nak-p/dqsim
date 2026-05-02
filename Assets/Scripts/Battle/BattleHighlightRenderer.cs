using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace DQSim.Battle
{
    public class BattleHighlightRenderer : MonoBehaviour
    {
        public Tilemap highlightTilemap;
        private Tile _highlightTile;

        private void Awake()
        {
            InitializeTilemap();
        }

        private void InitializeTilemap()
        {
            if (highlightTilemap == null) return;
            
            // Ensure the tilemap is centered and aligned with the grid
            highlightTilemap.tileAnchor = new Vector3(0.5f, 0.5f, 0f);
            highlightTilemap.transform.localPosition = Vector3.zero;
            highlightTilemap.transform.localRotation = Quaternion.identity;
            highlightTilemap.transform.localScale = Vector3.one;

            var tmr = highlightTilemap.GetComponent<UnityEngine.Tilemaps.TilemapRenderer>();
            if (tmr != null)
            {
                var rend = tmr.GetComponent<Renderer>();
                if (rend != null)
                {
                    rend.sortingOrder = 110;
                }
                // Material setup to match terrain visibility
                BattleHexTilemapRenderer.ApplyUnlitMaterialForUrp2D(tmr);
            }
        }

        public void ClearHighlights()
        {
            if (highlightTilemap != null)
                highlightTilemap.ClearAllTiles();
        }

        public void HighlightTiles(IEnumerable<Vector2Int> positions)
        {
            if (_highlightTile == null) CreateHexHighlightTile();
            
            ClearHighlights();
            if (highlightTilemap == null || _highlightTile == null) return;

            foreach (var pos in positions)
            {
                highlightTilemap.SetTile(new Vector3Int(pos.x, pos.y, 0), _highlightTile);
            }
        }

        private void CreateHexHighlightTile()
        {
            const float ppu = 48f;
            var grid = highlightTilemap != null ? highlightTilemap.GetComponentInParent<Grid>() : null;
            Vector3 cs = grid != null ? grid.cellSize : new Vector3(0.8660254f, 1f, 1f);

            int texH = Mathf.Max(16, Mathf.RoundToInt(cs.y * ppu));
            if (texH % 2 != 0) texH++;
            int texW = Mathf.Max(16, Mathf.RoundToInt(cs.x * ppu));
            if (texW % 2 != 0) texW++;

            var tex = new Texture2D(texW, texH) { filterMode = FilterMode.Point };
            tex.alphaIsTransparency = true;
            
            float cx = (texW - 1) * 0.5f;
            float cy = (texH - 1) * 0.5f;

            float rFromHeight = (texH - 1) * 0.5f;
            float rFromWidth = (texW - 1) / Mathf.Sqrt(3f);
            float rInscribed = Mathf.Min(rFromHeight, rFromWidth);
            float r = Mathf.Max(2f, rInscribed * 0.88f);

            Color32 highlightColor = new Color32(50, 150, 255, 140);
            Color32 clear = new Color32(0, 0, 0, 0);
            var pixels = new Color32[texW * texH];

            for (int y = 0; y < texH; y++)
            {
                for (int x = 0; x < texW; x++)
                {
                    pixels[y * texW + x] = InsidePointyTopHex(x + 0.5f, y + 0.5f, cx, cy, r) ? highlightColor : clear;
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply();

            _highlightTile = ScriptableObject.CreateInstance<Tile>();
            _highlightTile.sprite = Sprite.Create(
                tex, 
                new Rect(0, 0, texW, texH), 
                new Vector2(0.5f, 0.5f), 
                ppu, 
                0, 
                SpriteMeshType.Tight);
            _highlightTile.color = Color.white;
        }

        private static bool InsidePointyTopHex(float px, float py, float cx, float cy, float r)
        {
            const float eps = 1e-4f;
            var p = new Vector2(px, py);
            for (int i = 0; i < 6; i++)
            {
                float a1 = Mathf.PI / 2f + i * (Mathf.PI / 3f);
                float a2 = Mathf.PI / 2f + (i + 1) * (Mathf.PI / 3f);
                var v1 = new Vector2(cx + r * Mathf.Cos(a1), cy + r * Mathf.Sin(a1));
                var v2 = new Vector2(cx + r * Mathf.Cos(a2), cy + r * Mathf.Sin(a2));
                var edge = v2 - v1;
                var inward = p - v1;
                float cross = edge.x * inward.y - edge.y * inward.x;
                if (cross < -eps) return false;
            }
            return true;
        }
    }
}
