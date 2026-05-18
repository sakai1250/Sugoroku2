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

            if (_generateOnAwake && NeedsGeneration())
                GenerateLayeredBoard();
        }

        private bool NeedsGeneration()
        {
            var route = GetComponentInChildren<WaypointRoute>(true);
            return route == null || route.Count == 0;
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

            EventMassCatalog.EnsureLoaded();

            for (int i = 0; i < SnakeBoardLayout.CellCount; i++)
            {
                var pos     = SnakeBoardLayout.GetWorldPosition(i, _cellSpacing);
                var type    = SnakeBoardLayout.GetSquareType(i);
                var label   = SnakeBoardLayout.GetDisplayLabel(i);
                var eventId = SnakeBoardLayout.GetEventId(i);
                list.Add(SpawnWaypointInstance(routeRoot, pos, i, type, label, eventId));
            }

            route.SetWaypoints(list);
            return route;
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

        private static Transform CreateChild(Transform parent, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            return go.transform;
        }
    }
}
