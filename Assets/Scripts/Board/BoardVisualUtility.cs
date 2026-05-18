using UnityEngine;

namespace Sugoroku.Board
{
    /// <summary>URP でも確実に表示される 2D スプライト用ユーティリティ。</summary>
    public static class BoardVisualUtility
    {
        private static Material _spriteMaterial;
        private static Sprite   _squareSprite;
        private static Sprite   _circleSprite;
        private static Sprite   _starSprite;

        public static Material SpriteMaterial
        {
            get
            {
                if (_spriteMaterial != null) return _spriteMaterial;

                var shader =
                    Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default") ??
                    Shader.Find("Universal Render Pipeline/2D/Sprite-Lit-Default") ??
                    Shader.Find("Sprites/Default") ??
                    Shader.Find("Unlit/Texture");

                if (shader != null)
                    _spriteMaterial = new Material(shader);

                return _spriteMaterial;
            }
        }

        public static void ApplySpriteRenderer(SpriteRenderer sr, int sortingOrder = 0) =>
            ApplySpriteRenderer(sr, BoardSortingLayers.Board, sortingOrder);

        public static void ApplySpriteRenderer(SpriteRenderer sr, string sortingLayer, int sortingOrder = 0)
        {
            if (sr == null) return;

            if (!string.IsNullOrEmpty(sortingLayer) && SortingLayerExists(sortingLayer))
                sr.sortingLayerName = sortingLayer;

            sr.sortingOrder = sortingOrder;
            if (SpriteMaterial != null)
                sr.sharedMaterial = SpriteMaterial;
        }

        public static Sprite GetSquareSprite()
        {
            if (_squareSprite != null) return _squareSprite;

            const int size = 128;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
            var pixels = new Color[size * size];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.white;
            tex.SetPixels(pixels);
            tex.Apply();
            _squareSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 32f);
            return _squareSprite;
        }

        public static Sprite GetCircleSprite()
        {
            if (_circleSprite != null) return _circleSprite;

            const int size = 128;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
            var pixels = new Color[size * size];
            var center = new Vector2(size / 2f, size / 2f);
            float r = size / 2f - 4f;
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), center);
                float a = Mathf.Clamp01((r - d) / 3f);
                pixels[y * size + x] = new Color(1f, 1f, 1f, a);
            }
            tex.SetPixels(pixels);
            tex.Apply();
            _circleSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 32f);
            return _circleSprite;
        }

        /// <summary>ジャーナルマス用の簡易星形スプライト。</summary>
        public static Sprite GetStarSprite()
        {
            if (_starSprite != null) return _starSprite;

            const int size = 128;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
            var pixels = new Color[size * size];
            var center = new Vector2(size / 2f, size / 2f);

            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                var p = new Vector2(x, y) - center;
                float angle = Mathf.Atan2(p.y, p.x);
                float dist  = p.magnitude;
                float spikes = 5f;
                float r = (size * 0.38f) * (0.55f + 0.45f * Mathf.Cos(angle * spikes));
                float a = Mathf.Clamp01((r - dist) / 4f);
                pixels[y * size + x] = new Color(1f, 1f, 1f, a);
            }

            tex.SetPixels(pixels);
            tex.Apply();
            _starSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 32f);
            return _starSprite;
        }

        public static Sprite LoadKenneyPiece(string name) =>
            Visual.KenneyAssets.LoadSprite($"{Visual.KenneyAssets.BoardgamePack.PiecesRoot}/{name}");

        public static bool SortingLayerExists(string layerName)
        {
            if (string.IsNullOrEmpty(layerName)) return false;
            foreach (var layer in SortingLayer.layers)
                if (layer.name == layerName) return true;
            return false;
        }

        public static Bounds CalculateWaypointBounds(Waypoint[] waypoints, float padding = 1.5f)
        {
            if (waypoints == null || waypoints.Length == 0)
                return new Bounds(Vector3.zero, Vector3.one * 8f);

            var bounds = new Bounds(waypoints[0].transform.position, Vector3.zero);
            foreach (var wp in waypoints)
            {
                if (wp != null)
                    bounds.Encapsulate(wp.transform.position);
            }
            bounds.Expand(padding);
            return bounds;
        }
    }
}
