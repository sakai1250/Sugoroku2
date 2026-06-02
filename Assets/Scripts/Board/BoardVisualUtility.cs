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
        private static Sprite   _pixelSolidSprite;
        private static Sprite   _pixelCardSprite;
        private static Sprite   _pixelSparkleSprite;
        private static Sprite   _pixelCoinSprite;
        private static Sprite   _pixelPlatformBackdropSprite;
        private static Sprite   _softOvalShadowSprite;
        private static Sprite   _pixelCloudDepthSprite;
        private static Sprite   _pixelForegroundDepthSprite;

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

        public static Sprite GetPixelSolidSprite()
        {
            if (_pixelSolidSprite != null) return _pixelSolidSprite;

            const int size = 8;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            var pixels = new Color[size * size];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.white;
            tex.SetPixels(pixels);
            tex.Apply();
            _pixelSolidSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 8f);
            return _pixelSolidSprite;
        }

        public static Sprite GetPixelCardSprite()
        {
            if (_pixelCardSprite != null) return _pixelCardSprite;

            const int size = 32;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };

            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                bool outer = x < 2 || y < 2 || x >= size - 2 || y >= size - 2;
                bool frame = x < 5 || y < 5 || x >= size - 5 || y >= size - 5;
                bool highlight = y >= size - 6 || x < 7;
                bool shadow = y < 6 || x >= size - 7;
                bool checker = ((x / 4) + (y / 4)) % 2 == 0;

                float shade = outer ? 0.54f : frame ? 0.78f : checker ? 0.94f : 1f;
                if (highlight) shade = Mathf.Max(shade, 1f);
                if (shadow) shade *= 0.76f;

                tex.SetPixel(x, y, new Color(shade, shade, shade, 1f));
            }

            tex.Apply();
            _pixelCardSprite = Sprite.Create(
                tex,
                new Rect(0, 0, size, size),
                new Vector2(0.5f, 0.5f),
                32f,
                0,
                SpriteMeshType.FullRect,
                new Vector4(8f, 8f, 8f, 8f));
            return _pixelCardSprite;
        }

        public static Sprite GetPixelSparkleSprite()
        {
            if (_pixelSparkleSprite != null) return _pixelSparkleSprite;

            const int size = 9;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };

            var clear = new Color(1f, 1f, 1f, 0f);
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
                tex.SetPixel(x, y, clear);

            int c = size / 2;
            for (int i = 0; i < size; i++)
            {
                tex.SetPixel(c, i, Color.white);
                tex.SetPixel(i, c, Color.white);
            }
            tex.SetPixel(c - 1, c - 1, Color.white);
            tex.SetPixel(c + 1, c + 1, Color.white);
            tex.SetPixel(c - 1, c + 1, Color.white);
            tex.SetPixel(c + 1, c - 1, Color.white);

            tex.Apply();
            _pixelSparkleSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 9f);
            return _pixelSparkleSprite;
        }

        public static Sprite GetPixelCoinSprite()
        {
            if (_pixelCoinSprite != null) return _pixelCoinSprite;

            const int size = 13;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };

            var clear = new Color(1f, 1f, 1f, 0f);
            var center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                var p = new Vector2(x, y) - center;
                float d = p.magnitude;
                if (d > 5.6f)
                {
                    tex.SetPixel(x, y, clear);
                    continue;
                }

                bool rim = d > 4.25f;
                bool highlight = x <= 5 && y >= 7 && d < 4.8f;
                bool shadow = x >= 7 && y <= 4;
                float shade = rim ? 0.62f : 0.92f;
                if (highlight) shade = 1f;
                if (shadow) shade *= 0.72f;
                tex.SetPixel(x, y, new Color(shade, shade, shade, 1f));
            }

            tex.Apply();
            _pixelCoinSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 13f);
            return _pixelCoinSprite;
        }

        public static Sprite GetPixelPlatformBackdropSprite()
        {
            if (_pixelPlatformBackdropSprite != null) return _pixelPlatformBackdropSprite;

            const int width = 320;
            const int height = 180;
            var tex = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };

            var sky = new Color(0.44f, 0.76f, 0.94f, 1f);
            var skyBand = new Color(0.52f, 0.82f, 0.98f, 1f);
            var cloud = new Color(1f, 0.96f, 0.84f, 1f);
            var cloudShadow = new Color(0.86f, 0.88f, 0.84f, 1f);
            var hill = new Color(0.34f, 0.72f, 0.34f, 1f);
            var hillDark = new Color(0.20f, 0.54f, 0.28f, 1f);
            var grass = new Color(0.22f, 0.72f, 0.22f, 1f);
            var grassLight = new Color(0.58f, 0.90f, 0.22f, 1f);
            var dirt = new Color(0.56f, 0.32f, 0.16f, 1f);
            var dirtDark = new Color(0.34f, 0.18f, 0.10f, 1f);
            var brick = new Color(0.76f, 0.42f, 0.18f, 1f);
            var brickDark = new Color(0.42f, 0.20f, 0.12f, 1f);

            for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                bool band = y > 118 && ((y / 8) % 2 == 0);
                tex.SetPixel(x, y, band ? skyBand : sky);
            }

            DrawCloud(28, 124);
            DrawCloud(184, 134);
            DrawCloud(252, 108);
            DrawHill(74, 38, 116, 60, hillDark);
            DrawHill(224, 34, 146, 70, hill);
            DrawBrickRun(44, 92, 6);
            DrawBrickRun(208, 82, 5);
            DrawGround();

            tex.Apply();
            _pixelPlatformBackdropSprite = Sprite.Create(
                tex,
                new Rect(0, 0, width, height),
                new Vector2(0.5f, 0.5f),
                32f);
            return _pixelPlatformBackdropSprite;

            void FillRect(int sx, int sy, int w, int h, Color c)
            {
                int xMin = Mathf.Clamp(sx, 0, width);
                int yMin = Mathf.Clamp(sy, 0, height);
                int xMax = Mathf.Clamp(sx + w, 0, width);
                int yMax = Mathf.Clamp(sy + h, 0, height);
                for (int yy = yMin; yy < yMax; yy++)
                for (int xx = xMin; xx < xMax; xx++)
                    tex.SetPixel(xx, yy, c);
            }

            void DrawCloud(int x, int y)
            {
                FillRect(x + 8, y, 38, 12, cloudShadow);
                FillRect(x, y + 4, 54, 14, cloud);
                FillRect(x + 10, y + 14, 16, 12, cloud);
                FillRect(x + 28, y + 10, 20, 16, cloud);
            }

            void DrawHill(int cx, int baseY, int w, int h, Color c)
            {
                for (int yy = 0; yy < h; yy += 4)
                {
                    float t = yy / (float)h;
                    int half = Mathf.RoundToInt((w * 0.5f) * Mathf.Sqrt(1f - t));
                    FillRect(cx - half, baseY + yy, half * 2, 4, c);
                }

                for (int yy = 10; yy < h - 8; yy += 18)
                {
                    int half = Mathf.RoundToInt((w * 0.35f) * (1f - yy / (float)h));
                    FillRect(cx - half, baseY + yy, 8, 8, hillDark);
                    FillRect(cx + half - 8, baseY + yy + 5, 8, 8, hillDark);
                }
            }

            void DrawBrickRun(int x, int y, int count)
            {
                for (int i = 0; i < count; i++)
                {
                    int bx = x + i * 14;
                    FillRect(bx, y, 12, 12, brickDark);
                    FillRect(bx + 1, y + 1, 10, 10, brick);
                    FillRect(bx + 2, y + 8, 8, 2, new Color(0.98f, 0.66f, 0.28f, 1f));
                    FillRect(bx + 2, y + 2, 8, 2, brickDark);
                }
            }

            void DrawGround()
            {
                FillRect(0, 0, width, 31, dirt);
                FillRect(0, 31, width, 7, grass);
                FillRect(0, 36, width, 3, grassLight);
                for (int y = 2; y < 30; y += 8)
                for (int x = ((y / 8) % 2) * 8; x < width; x += 16)
                    FillRect(x, y, 8, 4, dirtDark);
            }
        }

        public static Sprite GetSoftOvalShadowSprite()
        {
            if (_softOvalShadowSprite != null) return _softOvalShadowSprite;

            const int width = 96;
            const int height = 48;
            var tex = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            var center = new Vector2(width * 0.5f, height * 0.5f);
            for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                var p = new Vector2((x - center.x) / (width * 0.5f), (y - center.y) / (height * 0.5f));
                float d = p.magnitude;
                float a = Mathf.SmoothStep(1f, 0f, d);
                a *= a;
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }

            tex.Apply();
            _softOvalShadowSprite = Sprite.Create(
                tex,
                new Rect(0, 0, width, height),
                new Vector2(0.5f, 0.5f),
                32f);
            return _softOvalShadowSprite;
        }

        public static Sprite GetPixelCloudDepthSprite()
        {
            if (_pixelCloudDepthSprite != null) return _pixelCloudDepthSprite;

            const int width = 320;
            const int height = 180;
            var tex = CreateTransparentPointTexture(width, height);
            var cloud = new Color(1f, 0.98f, 0.88f, 1f);
            var shade = new Color(0.72f, 0.76f, 0.82f, 1f);

            FillCloud(tex, 18, 118, cloud, shade);
            FillCloud(tex, 132, 136, cloud, shade);
            FillCloud(tex, 238, 104, cloud, shade);
            FillCloud(tex, 280, 146, cloud, shade);

            tex.Apply();
            _pixelCloudDepthSprite = Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 32f);
            return _pixelCloudDepthSprite;
        }

        public static Sprite GetPixelForegroundDepthSprite()
        {
            if (_pixelForegroundDepthSprite != null) return _pixelForegroundDepthSprite;

            const int width = 320;
            const int height = 180;
            var tex = CreateTransparentPointTexture(width, height);
            var dark = new Color(0.09f, 0.16f, 0.16f, 1f);
            var grass = new Color(0.12f, 0.46f, 0.20f, 1f);
            var light = new Color(0.42f, 0.82f, 0.28f, 1f);

            FillRect(tex, 0, 0, width, 18, dark);
            FillRect(tex, 0, 18, width, 6, grass);
            for (int x = 0; x < width; x += 16)
            {
                int h = 10 + (x % 48 == 0 ? 8 : 0);
                FillRect(tex, x + 2, 24, 4, h, light);
                FillRect(tex, x + 8, 24, 4, h - 4, grass);
            }

            tex.Apply();
            _pixelForegroundDepthSprite = Sprite.Create(
                tex,
                new Rect(0, 0, width, height),
                new Vector2(0.5f, 0.5f),
                32f);
            return _pixelForegroundDepthSprite;
        }

        private static Texture2D CreateTransparentPointTexture(int width, int height)
        {
            var tex = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };

            var clear = new Color(1f, 1f, 1f, 0f);
            for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
                tex.SetPixel(x, y, clear);
            return tex;
        }

        private static void FillCloud(Texture2D tex, int x, int y, Color color, Color shade)
        {
            FillRect(tex, x + 8, y, 42, 10, shade);
            FillRect(tex, x, y + 4, 58, 13, color);
            FillRect(tex, x + 12, y + 15, 16, 12, color);
            FillRect(tex, x + 30, y + 11, 22, 16, color);
        }

        private static void FillRect(Texture2D tex, int sx, int sy, int w, int h, Color c)
        {
            int width = tex.width;
            int height = tex.height;
            int xMin = Mathf.Clamp(sx, 0, width);
            int yMin = Mathf.Clamp(sy, 0, height);
            int xMax = Mathf.Clamp(sx + w, 0, width);
            int yMax = Mathf.Clamp(sy + h, 0, height);
            for (int yy = yMin; yy < yMax; yy++)
            for (int xx = xMin; xx < xMax; xx++)
                tex.SetPixel(xx, yy, c);
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
