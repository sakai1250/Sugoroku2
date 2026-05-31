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

        public void AlignBackgroundToRoute(WaypointRoute route, bool dimBackground = true)
        {
            if (route == null || route.Count == 0) return;

            var wpArray = new Waypoint[route.Count];
            for (int i = 0; i < route.Count; i++) wpArray[i] = route.GetWaypoint(i);

            var bounds = BoardVisualUtility.CalculateWaypointBounds(wpArray, 0.5f);
            bounds.Expand(_backgroundPadding);

            if (_backgroundArt == null)
                _backgroundArt = FindBackgroundRenderer();

            if (_backgroundArt == null) return;

            _backgroundArt.transform.position = new Vector3(bounds.center.x, bounds.center.y, 0f);
            BoardVisualUtility.ApplySpriteRenderer(
                _backgroundArt, BoardSortingLayers.Background, -100);

            if (dimBackground)
                _backgroundArt.color = _dimmedBackgroundColor;

            if (_backgroundArt.sprite != null)
            {
                var size = _backgroundArt.sprite.bounds.size;
                float scaleX = bounds.size.x / Mathf.Max(size.x, 0.01f);
                float scaleY = bounds.size.y / Mathf.Max(size.y, 0.01f);
                float scale  = Mathf.Max(scaleX, scaleY);
                _backgroundArt.transform.localScale = new Vector3(scale, scale, 1f);
            }
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
                if (sr.sortingLayerName == BoardSortingLayers.Background ||
                    sr.gameObject.name.Contains("Background") ||
                    sr.gameObject.name.Contains("Scroll"))
                    return sr;
            }

            return null;
        }
    }
}
