using System.Collections.Generic;
using UnityEngine;
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
        [SerializeField] private bool  _centerRouteOnBoard = true;
        [SerializeField] private bool  _dimBackgroundArt   = true;
        [SerializeField] private bool  _avoidCardOverlap   = true;
        [SerializeField] private float _overlapOffsetStep  = 0.42f;

        [Header("ルート表示")]
        [SerializeField] private bool  _buildRouteConnectors = true;
        [SerializeField] private float _connectorWidth       = 0.28f;
        [SerializeField] private Color _connectorColor       = new(0.30f, 0.72f, 0.24f, 0.94f);
        [SerializeField] private Color _connectorEdgeColor   = new(0.38f, 0.22f, 0.12f, 0.84f);

        [Header("プレハブ")]
        [SerializeField] private Waypoint _waypointPrefab;
        [SerializeField] private bool _preferTextCardPrefab = true;

        [Header("参照")]
        [SerializeField] private BoardManager _boardManager;

        private Transform _boardRoot;

        // カード実寸ベースの密集間隔を使う（レガシーの _cellSpacing には依存しない）。
        private float SpacingX => MassTextCardPrefabFactory.RecommendedSpacingX;
        private float SpacingY => MassTextCardPrefabFactory.RecommendedSpacingY;
        private float RouteSpacing => Mathf.Max(SpacingX, SpacingY);

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
            return route == null || route.Count == 0 || route.Count != BoardNavigation.PhysicalWaypointCount;
        }

        private void RefreshExistingRoutePresentation()
        {
            var route = GetComponentInChildren<WaypointRoute>(true);
            if (route == null || route.Count == 0) return;

            route.SyncRouteIndices();
            RelayoutRouteToSnakeGrid(route);

            if (_boardManager != null)
                _boardManager.SetRoute(route);
            ApplyOverlapAvoidanceToRoute(route);
            ApplyBranchRouteStyling(route);
            if (_buildRouteConnectors)
                BuildRouteConnectors(route.transform, route);

            var depth = GetComponent<BoardDepthPresentation>();
            if (depth == null) depth = gameObject.AddComponent<BoardDepthPresentation>();
            depth.Rebuild(route);

            var cam = FindFirstObjectByType<BoardCameraController>();
            cam?.FrameBoard();
        }

        /// <summary>分岐点・両レーンのカードを水色のソリッド表示にする（参照画像準拠）。</summary>
        private static void ApplyBranchRouteStyling(WaypointRoute route)
        {
            if (route == null) return;
            for (int p = 0; p < route.Count; p++)
            {
                var wp = route.GetWaypoint(p);
                if (wp == null) continue;
                bool isBranch = BoardNavigation.GetLane(p) != BranchRoute.None
                                || wp.Type == SquareType.Branch;
                if (isBranch) wp.ApplyBranchRouteStyle();
            }
        }

        /// <summary>シーンに手置きされたマスを S字グリッドへ再配置（重なり解消）。</summary>
        private void RelayoutRouteToSnakeGrid(WaypointRoute route)
        {
            if (route == null || route.Count == 0) return;

            var placedPositions = new List<Vector3>(route.Count);

            for (int p = 0; p < route.Count; p++)
            {
                var wp = route.GetWaypoint(p);
                if (wp == null) continue;

                int logical = BoardNavigation.GetLogical(p);
                var lane    = BoardNavigation.GetLane(p);
                var basePos = BoardNavigation.GetWorldPosition(logical, lane, SpacingX, SpacingY);
                var pos     = ResolveCardPosition(basePos, p, placedPositions);
                wp.transform.position = pos;
                wp.SetCellSpacing(RouteSpacing);
                placedPositions.Add(pos);
            }

            if (_centerRouteOnBoard)
                route.CenterRouteAtOrigin();
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

            ApplyOverlapAvoidanceToRoute(route);

            if (_buildRouteConnectors)
                BuildRouteConnectors(routeRoot, route);

            var env = GetComponent<BoardEnvironment>();
            if (env == null) env = gameObject.AddComponent<BoardEnvironment>();
            env.AlignBackgroundToRoute(route, _dimBackgroundArt);

            var depth = GetComponent<BoardDepthPresentation>();
            if (depth == null) depth = gameObject.AddComponent<BoardDepthPresentation>();
            depth.Rebuild(route);

            if (_boardManager != null)
                _boardManager.SetRoute(route);

            ApplyBranchRouteStyling(route);

            var cam = FindFirstObjectByType<BoardCameraController>();
            cam?.FrameBoard();

            Debug.Log($"SnakeBoard: {route.Count} ウェイポイント（論理{BoardNavigation.LogicalCellCount}マス・分岐レーン）を配置しました（間隔 X={SpacingX:F2} Y={SpacingY:F2}）。");
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

            var list = new List<Waypoint>(BoardNavigation.PhysicalWaypointCount);
            var placedPositions = new List<Vector3>(BoardNavigation.PhysicalWaypointCount);

            EventMassCatalog.EnsureLoaded();
            EventMasuArt.Prewarm();

            for (int p = 0; p < BoardNavigation.PhysicalWaypointCount; p++)
            {
                int logical = BoardNavigation.GetLogical(p);
                var lane    = BoardNavigation.GetLane(p);
                var basePos = BoardNavigation.GetWorldPosition(logical, lane, SpacingX, SpacingY);
                var pos     = ResolveCardPosition(basePos, p, placedPositions);
                var type    = BoardNavigation.GetSquareType(logical, lane);
                var label   = BoardNavigation.GetDisplayLabel(logical, lane);
                var eventId = BoardNavigation.GetEventId(logical, lane);
                list.Add(SpawnWaypointInstance(routeRoot, pos, p, lane, type, label, eventId));
                placedPositions.Add(pos);
            }

            route.SetWaypoints(list);
            return route;
        }

        private Vector3 ResolveCardPosition(Vector3 basePos, int physicalIndex, List<Vector3> placedPositions)
        {
            // 分岐レーンは専用レイアウトで間隔を確保済み
            if (BoardNavigation.GetLane(physicalIndex) != BranchRoute.None)
                return basePos;

            if (!_avoidCardOverlap || placedPositions == null || placedPositions.Count == 0)
                return basePos;
            if (!OverlapsAnyCard(basePos, placedPositions))
                return basePos;

            var previousBase = physicalIndex > 0 && placedPositions.Count > 0
                ? placedPositions[placedPositions.Count - 1]
                : BoardNavigation.GetWorldPosition(0, BranchRoute.None, SpacingX, SpacingY);
            return ResolvePositionAgainstPlaced(basePos, basePos - previousBase, physicalIndex, placedPositions);
        }

        private void ApplyOverlapAvoidanceToRoute(WaypointRoute route)
        {
            if (!_avoidCardOverlap || route == null || route.Count < 2) return;

            var placedPositions = new List<Vector3>(route.Count);
            for (int i = 0; i < route.Count; i++)
            {
                var wp = route.GetWaypoint(i);
                if (wp == null) continue;

                if (BoardNavigation.GetLane(i) != BranchRoute.None)
                {
                    placedPositions.Add(wp.transform.position);
                    continue;
                }

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
            // 密集グリッドでは隣接カードは 1 セル分だけ離れて接する。真の重なり
            // （中央がカード実寸の 85% より近い）だけを検出し、格子配置は許容する。
            float minXDistance = MassTextCardPrefabFactory.CardWorldWidth  * 0.85f;
            float minYDistance = MassTextCardPrefabFactory.CardWorldHeight * 0.85f;

            foreach (var other in placedPositions)
            {
                if (Mathf.Abs(pos.x - other.x) < minXDistance &&
                    Mathf.Abs(pos.y - other.y) < minYDistance)
                    return true;
            }

            return false;
        }

        private Waypoint SpawnWaypointInstance(Transform parent, Vector3 pos, int physicalIndex,
            BranchRoute lane, SquareType type, string label, string eventId)
        {
            string laneTag = lane == BranchRoute.None ? "" : lane == BranchRoute.Lab ? "_Lab" : "_PT";
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
                wp = MassTextCardPrefabFactory.CreateRuntimeInstance(parent, $"W{physicalIndex:D2}{laneTag}");
            }

            wp.transform.position = pos;
            wp.SetCellSpacing(RouteSpacing);
            wp.RemoveWorldLabels();
            wp.Configure(physicalIndex, type, label, eventId);
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

            var segments = new List<(Vector3 from, Vector3 to)>();
            BoardNavigation.CollectConnectorSegments(route, segments);

            for (int i = 0; i < segments.Count; i++)
            {
                var (from, to) = segments[i];
                CreateConnectorSegment(connectorsRoot, $"RouteConnector_{i:D2}_Edge",
                    from, to, _connectorWidth + 0.16f, _connectorEdgeColor, BoardSortingLayers.PathOrder);
                CreateConnectorSegment(connectorsRoot, $"RouteConnector_{i:D2}",
                    from, to, _connectorWidth, _connectorColor, BoardSortingLayers.PathOrder + 1);
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
            sr.sprite = BoardVisualUtility.GetPixelSolidSprite();
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
