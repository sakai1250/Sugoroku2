using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using Sugoroku.Data;

namespace Sugoroku.Board
{
    /// <summary>
    /// Route_Main 配下に Waypoint_Base を S字ルート（20マス）で等間隔配置する。
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-50)]
    public class LayeredBoardGenerator : MonoBehaviour
    {
        public const string WaypointPrefabPath    = "Assets/Prefabs/Board/Waypoint_Base.prefab";
        public const string MassTextCardPrefabPath = "Assets/Prefabs/Board/Mass_TextCard.prefab";

        [Header("生成")]
        [SerializeField] private bool  _generateOnAwake    = true;
        [FormerlySerializedAs("_tileWorldWidth")]
        [SerializeField] private float _cellSpacing        = 3.0f;
        [SerializeField] private bool  _centerRouteOnBoard = true;
        [SerializeField] private bool  _dimBackgroundArt   = true;
        [SerializeField] private bool  _avoidCardOverlap   = true;
        [SerializeField] private float _overlapOffsetStep  = 0.42f;

        [Header("ルート表示")]
        [SerializeField] private bool  _buildRouteConnectors = true;
        [SerializeField] private float _connectorWidth       = 0.28f;
        [SerializeField] private Color _connectorColor       = new(0.86f, 0.88f, 0.78f, 0.92f);
        [SerializeField] private Color _connectorEdgeColor   = new(0.36f, 0.42f, 0.46f, 0.72f);

        [Header("プレハブ")]
        [SerializeField] private Waypoint _waypointPrefab;
        [SerializeField] private bool _preferTextCardPrefab = true;

        [Header("参照")]
        [SerializeField] private BoardManager _boardManager;

        private Transform _boardRoot;

        private void Awake()
        {
            if (_boardManager == null)
                _boardManager = GetComponent<BoardManager>() ?? GetComponentInParent<BoardManager>();

            if (_generateOnAwake)
            {
                if (NeedsGeneration())
                {
                    GenerateLayeredBoard();
                }
                else
                {
                    RefreshExistingRoutePresentation();
                }
            }
        }

        private bool NeedsGeneration()
        {
            var route = GetComponentInChildren<WaypointRoute>(true);
            return route == null || route.Count == 0;
        }

        private void RefreshExistingRoutePresentation()
        {
            var route = GetComponentInChildren<WaypointRoute>(true);
            if (route == null || route.Count == 0) return;

            if (_boardManager != null)
                _boardManager.SetRoute(route);
            ApplyOverlapAvoidanceToRoute(route);
            if (_buildRouteConnectors)
                BuildRouteConnectors(route.transform, route);
        }

        public void GenerateLayeredBoard()
        {
            ClearLayeredBoard();
            RemoveStrayBoardObjects();
            ResolveWaypointPrefab();

            _boardRoot = new GameObject("SnakeBoard").transform;
            _boardRoot.SetParent(transform, false);

            var routeRoot = CreateChild(_boardRoot, "Route_Main");
            var route     = SpawnRoute(routeRoot);

            if (_centerRouteOnBoard)
                route.CenterRouteAtOrigin();

            if (_buildRouteConnectors)
                BuildRouteConnectors(routeRoot, route);

            var env = GetComponent<BoardEnvironment>();
            if (env == null) env = gameObject.AddComponent<BoardEnvironment>();
            env.AlignBackgroundToRoute(route, _dimBackgroundArt);

            if (_boardManager != null)
                _boardManager.SetRoute(route);

            var cam = FindFirstObjectByType<BoardCameraController>();
            cam?.FrameBoard();

            Debug.Log($"SnakeBoard: {route.Count} マスを S字ルートに配置しました（間隔 {_cellSpacing}）。");
        }

        public void ClearLayeredBoard()
        {
            foreach (var name in new[] { "SnakeBoard", "LayeredBoard" })
            {
                var existing = transform.Find(name);
                if (existing == null) continue;
                if (Application.isPlaying) Destroy(existing.gameObject);
                else DestroyImmediate(existing.gameObject);
            }
            _boardRoot = null;
        }

        /// <summary>古い生成物・Kenney 直置きなどを除去（BackgroundArt は残す）。</summary>
        public void RemoveStrayBoardObjects()
        {
            foreach (var name in new[]
            {
                "SafeDioramaBoard", "HexagonKitField", "BoardBase", "Tiles",
                "Waypoints", "P1Waypoints", "ManualRoute", "LayeredBoard", "SnakeBoard"
            })
            {
                var t = transform.Find(name);
                if (t == null) continue;
                if (Application.isPlaying) Destroy(t.gameObject);
                else DestroyImmediate(t.gameObject);
            }
        }

        private void ResolveWaypointPrefab()
        {
            if (_waypointPrefab != null) return;
#if UNITY_EDITOR
            if (_preferTextCardPrefab)
                _waypointPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<Waypoint>(MassTextCardPrefabPath);
            if (_waypointPrefab == null)
                _waypointPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<Waypoint>(WaypointPrefabPath);
#endif
        }

        private WaypointRoute SpawnRoute(Transform routeRoot)
        {
            var route = routeRoot.GetComponent<WaypointRoute>();
            if (route == null) route = routeRoot.gameObject.AddComponent<WaypointRoute>();

            var list = new List<Waypoint>(SnakeBoardLayout.CellCount);
            var placedPositions = new List<Vector3>(SnakeBoardLayout.CellCount);

            EventMassCatalog.EnsureLoaded();

            for (int i = 0; i < SnakeBoardLayout.CellCount; i++)
            {
                var basePos = SnakeBoardLayout.GetWorldPosition(i, _cellSpacing);
                var pos     = ResolveCardPosition(basePos, i, placedPositions);
                var type    = SnakeBoardLayout.GetSquareType(i);
                var label   = SnakeBoardLayout.GetDisplayLabel(i);
                var eventId = SnakeBoardLayout.GetEventId(i);
                list.Add(SpawnWaypointInstance(routeRoot, pos, i, type, label, eventId));
                placedPositions.Add(pos);
            }

            route.SetWaypoints(list);
            return route;
        }

        private Vector3 ResolveCardPosition(Vector3 basePos, int index, List<Vector3> placedPositions)
        {
            if (!_avoidCardOverlap || placedPositions == null || placedPositions.Count == 0)
                return basePos;
            if (!OverlapsAnyCard(basePos, placedPositions))
                return basePos;

            var previousBase = SnakeBoardLayout.GetWorldPosition(index - 1, _cellSpacing);
            return ResolvePositionAgainstPlaced(basePos, basePos - previousBase, index, placedPositions);
        }

        private void ApplyOverlapAvoidanceToRoute(WaypointRoute route)
        {
            if (!_avoidCardOverlap || route == null || route.Count < 2) return;

            var placedPositions = new List<Vector3>(route.Count);
            for (int i = 0; i < route.Count; i++)
            {
                var wp = route.GetWaypoint(i);
                if (wp == null) continue;

                var pos = wp.transform.position;
                if (placedPositions.Count > 0 && OverlapsAnyCard(pos, placedPositions))
                {
                    var prev = route.GetWaypoint(Mathf.Max(i - 1, 0));
                    var previousPos = prev != null
                        ? prev.transform.position
                        : placedPositions[placedPositions.Count - 1];
                    pos = ResolvePositionAgainstPlaced(pos, pos - previousPos, i, placedPositions);
                    wp.transform.position = pos;
                }

                placedPositions.Add(pos);
            }
        }

        private Vector3 ResolvePositionAgainstPlaced(Vector3 basePos, Vector3 direction, int index,
            List<Vector3> placedPositions)
        {
            var offsetAxis = Mathf.Abs(direction.x) >= Mathf.Abs(direction.y) ? Vector3.up : Vector3.right;
            if ((index & 1) == 1) offsetAxis *= -1f;

            for (int step = 1; step <= 6; step++)
            {
                float amount = _overlapOffsetStep * ((step + 1) / 2);
                float sign = (step & 1) == 1 ? 1f : -1f;
                var candidate = basePos + offsetAxis * (amount * sign);
                if (!OverlapsAnyCard(candidate, placedPositions))
                    return candidate;
            }

            return basePos + offsetAxis * (_overlapOffsetStep * 3f);
        }

        private static bool OverlapsAnyCard(Vector3 pos, List<Vector3> placedPositions)
        {
            const float margin = 0.18f;
            float minXDistance = MassTextCardPrefabFactory.CardWorldWidth + margin;
            float minYDistance = MassTextCardPrefabFactory.CardWorldHeight + margin;

            foreach (var other in placedPositions)
            {
                if (Mathf.Abs(pos.x - other.x) < minXDistance &&
                    Mathf.Abs(pos.y - other.y) < minYDistance)
                    return true;
            }

            return false;
        }

        private Waypoint SpawnWaypointInstance(Transform parent, Vector3 pos, int index,
            SquareType type, string label, string eventId)
        {
            Waypoint wp;
            if (_waypointPrefab != null)
            {
#if UNITY_EDITOR
                if (!Application.isPlaying)
                    wp = UnityEditor.PrefabUtility.InstantiatePrefab(_waypointPrefab, parent) as Waypoint;
                else
#endif
                    wp = Instantiate(_waypointPrefab, parent);
            }
            else
            {
                wp = MassTextCardPrefabFactory.CreateRuntimeInstance(parent, $"W{index:D2}");
            }

            wp.transform.position = pos;
            wp.SetCellSpacing(_cellSpacing);
            wp.RemoveWorldLabels();
            wp.Configure(index, type, label, eventId);
            return wp;
        }

        private void BuildRouteConnectors(Transform routeRoot, WaypointRoute route)
        {
            if (routeRoot == null || route == null || route.Count < 2) return;

            var existing = routeRoot.Find("RouteConnectors");
            if (existing != null)
            {
                if (Application.isPlaying) Destroy(existing.gameObject);
                else DestroyImmediate(existing.gameObject);
            }

            var connectorsRoot = CreateChild(routeRoot, "RouteConnectors");
            connectorsRoot.SetAsFirstSibling();

            for (int i = 0; i < route.Count - 1; i++)
            {
                var from = route.GetWaypoint(i);
                var to   = route.GetWaypoint(i + 1);
                if (from == null || to == null) continue;

                CreateConnectorSegment(connectorsRoot, $"RouteConnector_{i:D2}_Edge",
                    from.transform.position, to.transform.position,
                    _connectorWidth + 0.16f, _connectorEdgeColor, BoardSortingLayers.PathOrder);
                CreateConnectorSegment(connectorsRoot, $"RouteConnector_{i:D2}",
                    from.transform.position, to.transform.position,
                    _connectorWidth, _connectorColor, BoardSortingLayers.PathOrder + 1);
            }
        }

        private static void CreateConnectorSegment(Transform parent, string name, Vector3 from, Vector3 to,
            float width, Color color, int sortingOrder)
        {
            var delta = to - from;
            float length = delta.magnitude;
            if (length <= 0.01f) return;

            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = (from + to) * 0.5f;
            go.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = BoardVisualUtility.GetSquareSprite();
            sr.color = color;
            var spriteSize = sr.sprite != null ? sr.sprite.bounds.size : Vector3.one;
            go.transform.localScale = new Vector3(
                length / Mathf.Max(spriteSize.x, 0.01f),
                width / Mathf.Max(spriteSize.y, 0.01f),
                1f);
            BoardVisualUtility.ApplySpriteRenderer(sr, BoardSortingLayers.Board, sortingOrder);
        }

        private static Transform CreateChild(Transform parent, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            return go.transform;
        }
    }
}
