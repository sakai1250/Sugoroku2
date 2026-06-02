using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Sugoroku.Board
{
    /// <summary>2D盤面に、接地影と床面ラインで疑似3Dの奥行きを足す。</summary>
    [DisallowMultipleComponent]
    public class BoardDepthPresentation : MonoBehaviour
    {
        [SerializeField] private bool  _buildOnStart = true;
        [SerializeField] private float _shadowOffsetX = 0.18f;
        [SerializeField] private float _shadowOffsetY = -0.16f;
        [SerializeField] private float _shadowWidth = 2.12f;
        [SerializeField] private float _shadowHeight = 0.42f;
        [SerializeField] private float _floorAlpha = 0.13f;

        private readonly List<ShadowBinding> _shadows = new();
        private Transform _depthRoot;
        private Coroutine _waitRoutine;

        private void Start()
        {
            if (_buildOnStart)
                Rebuild(BoardManager.Instance?.Route ?? GetComponentInChildren<WaypointRoute>(true));
        }

        private void OnDisable()
        {
            if (_waitRoutine != null)
            {
                StopCoroutine(_waitRoutine);
                _waitRoutine = null;
            }
        }

        private void LateUpdate()
        {
            if (_shadows.Count == 0) return;

            float time = Time.time;
            foreach (var shadow in _shadows)
            {
                if (shadow.Waypoint == null || shadow.Renderer == null) continue;

                float pulse = 0.5f + 0.5f * Mathf.Sin(time * 1.6f + shadow.Seed);
                shadow.Renderer.transform.position = shadow.Waypoint.transform.position +
                    new Vector3(_shadowOffsetX, _shadowOffsetY, 0f);
                shadow.Renderer.transform.localScale = shadow.BaseScale * (0.97f + pulse * 0.035f);
                shadow.Renderer.color = new Color(0f, 0f, 0f, shadow.BaseAlpha * (0.88f + pulse * 0.12f));
            }
        }

        public void Rebuild(WaypointRoute route)
        {
            if (route == null || route.Count == 0)
            {
                if (Application.isPlaying && isActiveAndEnabled && _waitRoutine == null)
                    _waitRoutine = StartCoroutine(RebuildWhenReady());
                return;
            }

            if (_waitRoutine != null)
            {
                StopCoroutine(_waitRoutine);
                _waitRoutine = null;
            }

            ClearDepthRoot();
            BuildDepth(route);
        }

        private IEnumerator RebuildWhenReady()
        {
            while (BoardManager.Instance == null || BoardManager.Instance.Route == null ||
                   BoardManager.Instance.Route.Count == 0)
                yield return null;

            _waitRoutine = null;
            Rebuild(BoardManager.Instance.Route);
        }

        private void BuildDepth(WaypointRoute route)
        {
            _depthRoot = new GameObject("Faux3DDepth").transform;
            _depthRoot.SetParent(transform, false);
            _depthRoot.SetAsFirstSibling();

            CreateFloorPlane(route);
            CreateWaypointShadows(route);
        }

        private void CreateFloorPlane(WaypointRoute route)
        {
            var bounds = route.GetRouteBounds(1.65f);

            var floor = CreateRenderer("DepthFloorPlane", BoardVisualUtility.GetSoftOvalShadowSprite(),
                new Color(0f, 0f, 0f, _floorAlpha), BoardSortingLayers.PathOrder - 8);
            floor.transform.position = new Vector3(bounds.center.x + 0.16f, bounds.center.y - 0.30f, 0f);
            SetWorldSize(floor.transform, bounds.size.x * 1.10f, Mathf.Max(bounds.size.y * 0.88f, 1.2f));

            const int lineCount = 7;
            for (int i = 0; i < lineCount; i++)
            {
                float t = i / (float)(lineCount - 1);
                float y = Mathf.Lerp(bounds.min.y + 0.3f, bounds.max.y - 0.3f, t);
                float width = Mathf.Lerp(bounds.size.x * 1.02f, bounds.size.x * 0.66f, t);
                var line = CreateRenderer($"DepthFloorLine_{i:D2}", BoardVisualUtility.GetPixelSolidSprite(),
                    new Color(1f, 1f, 1f, Mathf.Lerp(0.04f, 0.085f, t)), BoardSortingLayers.PathOrder - 7);
                line.transform.position = new Vector3(bounds.center.x, y, 0f);
                SetWorldSize(line.transform, width, 0.035f);
            }
        }

        private void CreateWaypointShadows(WaypointRoute route)
        {
            _shadows.Clear();

            for (int i = 0; i < route.Count; i++)
            {
                var wp = route.GetWaypoint(i);
                if (wp == null) continue;

                var sr = CreateRenderer($"WaypointDepthShadow_{i:D2}", BoardVisualUtility.GetSoftOvalShadowSprite(),
                    new Color(0f, 0f, 0f, 0.24f), BoardSortingLayers.WaypointBaseOrder - 14);
                sr.transform.position = wp.transform.position + new Vector3(_shadowOffsetX, _shadowOffsetY, 0f);
                SetWorldSize(sr.transform, _shadowWidth, _shadowHeight);

                _shadows.Add(new ShadowBinding
                {
                    Waypoint = wp,
                    Renderer = sr,
                    BaseScale = sr.transform.localScale,
                    BaseAlpha = 0.22f,
                    Seed = i * 0.57f
                });
            }
        }

        private SpriteRenderer CreateRenderer(string objectName, Sprite sprite, Color color, int order)
        {
            var go = new GameObject(objectName);
            go.transform.SetParent(_depthRoot, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = color;
            BoardVisualUtility.ApplySpriteRenderer(sr, BoardSortingLayers.Board, order);
            return sr;
        }

        private void ClearDepthRoot()
        {
            _shadows.Clear();

            var existing = transform.Find("Faux3DDepth");
            if (existing == null) return;
            if (Application.isPlaying) Destroy(existing.gameObject);
            else DestroyImmediate(existing.gameObject);
        }

        private static void SetWorldSize(Transform target, float width, float height)
        {
            var sr = target.GetComponent<SpriteRenderer>();
            var spriteSize = sr != null && sr.sprite != null ? sr.sprite.bounds.size : Vector3.one;
            target.localScale = new Vector3(
                width / Mathf.Max(spriteSize.x, 0.01f),
                height / Mathf.Max(spriteSize.y, 0.01f),
                1f);
        }

        private sealed class ShadowBinding
        {
            public Waypoint Waypoint;
            public SpriteRenderer Renderer;
            public Vector3 BaseScale;
            public float BaseAlpha;
            public float Seed;
        }
    }
}
