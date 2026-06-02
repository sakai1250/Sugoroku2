using UnityEngine;

namespace Sugoroku.Board
{
    /// <summary>
    /// 学位記などの背景イラストをルートの背後に配置し、マス目と重ならないようにする。
    /// </summary>
    [DisallowMultipleComponent]
    public class BoardEnvironment : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer _backgroundArt;
        [SerializeField] private float          _backgroundPadding = 1.4f;
        [SerializeField] private Color          _dimmedBackgroundColor = new(0.72f, 0.76f, 0.84f, 0.42f);
        [SerializeField] private bool           _usePixelPlatformBackdrop = true;
        [SerializeField] private Color          _pixelBackdropColor = new(1f, 1f, 1f, 0.96f);
        [SerializeField] private bool           _useFauxDepthParallax = true;
        [SerializeField] private float          _cloudParallax = 0.08f;
        [SerializeField] private float          _foregroundParallax = 0.20f;

        private SpriteRenderer _cloudDepthLayer;
        private SpriteRenderer _foregroundDepthLayer;
        private Vector3 _parallaxCameraBase;
        private Vector3 _cloudLayerBase;
        private Vector3 _foregroundLayerBase;
        private bool _parallaxReady;

        public void AlignBackgroundToRoute(WaypointRoute route, bool dimBackground = true)
        {
            if (route == null || route.Count == 0) return;

            var wpArray = new Waypoint[route.Count];
            for (int i = 0; i < route.Count; i++) wpArray[i] = route.GetWaypoint(i);

            var bounds = BoardVisualUtility.CalculateWaypointBounds(wpArray, 0.5f);
            bounds.Expand(_backgroundPadding);

            if (_backgroundArt == null)
                _backgroundArt = FindBackgroundRenderer();

            if (_backgroundArt == null && _usePixelPlatformBackdrop)
                _backgroundArt = CreatePixelBackgroundRenderer();

            if (_backgroundArt == null) return;

            if (_usePixelPlatformBackdrop)
                _backgroundArt.sprite = BoardVisualUtility.GetPixelPlatformBackdropSprite();

            _backgroundArt.transform.position = new Vector3(bounds.center.x, bounds.center.y, 0f);
            BoardVisualUtility.ApplySpriteRenderer(
                _backgroundArt, BoardSortingLayers.Background, -100);

            if (_usePixelPlatformBackdrop)
                _backgroundArt.color = _pixelBackdropColor;
            else if (dimBackground)
                _backgroundArt.color = _dimmedBackgroundColor;

            float scale = 1f;
            if (_backgroundArt.sprite != null)
            {
                var size = _backgroundArt.sprite.bounds.size;
                float scaleX = bounds.size.x / Mathf.Max(size.x, 0.01f);
                float scaleY = bounds.size.y / Mathf.Max(size.y, 0.01f);
                scale = Mathf.Max(scaleX, scaleY);
                _backgroundArt.transform.localScale = new Vector3(scale, scale, 1f);
            }

            if (_usePixelPlatformBackdrop && _useFauxDepthParallax)
                EnsureFauxDepthLayers(bounds, scale);
        }

        private void LateUpdate()
        {
            if (!_parallaxReady || !_useFauxDepthParallax) return;

            var cam = Camera.main;
            if (cam == null) return;

            var delta = cam.transform.position - _parallaxCameraBase;
            if (_cloudDepthLayer != null)
                _cloudDepthLayer.transform.position = _cloudLayerBase + new Vector3(delta.x, delta.y, 0f) * _cloudParallax;
            if (_foregroundDepthLayer != null)
                _foregroundDepthLayer.transform.position = _foregroundLayerBase + new Vector3(delta.x, delta.y, 0f) * _foregroundParallax;
        }

        private SpriteRenderer FindBackgroundRenderer()
        {
            foreach (var name in new[] { "BackgroundArt", "Background", "学位記", "Diploma" })
            {
                var t = transform.Find(name);
                if (t != null && t.TryGetComponent<SpriteRenderer>(out var sr))
                    return sr;
            }

            foreach (var sr in GetComponentsInChildren<SpriteRenderer>(true))
            {
                if (sr.GetComponent<Waypoint>() != null) continue;
                if (sr.gameObject.name.Contains("DepthLayer") ||
                    sr.gameObject.name.Contains("DepthFloor") ||
                    sr.gameObject.name.Contains("PixelForeground"))
                    continue;
                if (sr.sortingLayerName == BoardSortingLayers.Background ||
                    sr.gameObject.name.Contains("Background") ||
                    sr.gameObject.name.Contains("Scroll"))
                    return sr;
            }

            return null;
        }

        private SpriteRenderer CreatePixelBackgroundRenderer()
        {
            var go = new GameObject("PixelPlatformBackdrop");
            go.transform.SetParent(transform, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = BoardVisualUtility.GetPixelPlatformBackdropSprite();
            return sr;
        }

        private void EnsureFauxDepthLayers(Bounds bounds, float backgroundScale)
        {
            _cloudDepthLayer ??= CreateDepthLayer("PixelCloudDepthLayer", BoardVisualUtility.GetPixelCloudDepthSprite(), -92);
            _foregroundDepthLayer ??= CreateDepthLayer("PixelForegroundDepthLayer", BoardVisualUtility.GetPixelForegroundDepthSprite(), -78);

            if (_cloudDepthLayer != null)
            {
                _cloudDepthLayer.transform.position = new Vector3(bounds.center.x, bounds.center.y + 0.35f, 0f);
                _cloudDepthLayer.transform.localScale = Vector3.one * (backgroundScale * 1.05f);
                _cloudDepthLayer.color = new Color(1f, 1f, 1f, 0.54f);
                _cloudLayerBase = _cloudDepthLayer.transform.position;
            }

            if (_foregroundDepthLayer != null)
            {
                _foregroundDepthLayer.transform.position = new Vector3(bounds.center.x, bounds.min.y - 0.35f, 0f);
                _foregroundDepthLayer.transform.localScale = Vector3.one * (backgroundScale * 1.08f);
                _foregroundDepthLayer.color = new Color(1f, 1f, 1f, 0.38f);
                _foregroundLayerBase = _foregroundDepthLayer.transform.position;
            }

            var cam = Camera.main;
            _parallaxCameraBase = cam != null ? cam.transform.position : Vector3.zero;
            _parallaxReady = true;
        }

        private SpriteRenderer CreateDepthLayer(string objectName, Sprite sprite, int sortingOrder)
        {
            var existing = transform.Find(objectName);
            SpriteRenderer sr;
            if (existing != null)
                sr = existing.GetComponent<SpriteRenderer>() ?? existing.gameObject.AddComponent<SpriteRenderer>();
            else
            {
                var go = new GameObject(objectName);
                go.transform.SetParent(transform, false);
                sr = go.AddComponent<SpriteRenderer>();
            }

            sr.sprite = sprite;
            BoardVisualUtility.ApplySpriteRenderer(sr, BoardSortingLayers.Background, sortingOrder);
            return sr;
        }
    }
}
