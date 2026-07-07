using System.Collections.Generic;
using UnityEngine;
using Sugoroku.Data;

namespace Sugoroku.Board
{
    /// <summary>
    /// ルート上のマスを配列順（0→1→2…）で一括管理。インデックス＝進行位置。
    /// </summary>
    [DisallowMultipleComponent]
    public class WaypointRoute : MonoBehaviour
    {
        [SerializeField] private List<Waypoint> waypoints = new();
        [SerializeField] private bool showRouteGizmos = true;

        public int Count => waypoints?.Count ?? 0;

        public IReadOnlyList<Waypoint> Waypoints => waypoints;

        public Waypoint GetWaypoint(int index)
        {
            if (waypoints == null || index < 0 || index >= waypoints.Count) return null;
            return waypoints[index];
        }

        public Vector3 GetPosition(int index)
        {
            var wp = GetWaypoint(index);
            return wp != null ? wp.transform.position : Vector3.zero;
        }

        public SquareType GetSquareType(int index)
        {
            var wp = GetWaypoint(index);
            return wp != null ? wp.Type : SquareType.Normal;
        }

        public void SetWaypoints(IList<Waypoint> ordered)
        {
            waypoints = ordered != null ? new List<Waypoint>(ordered) : new List<Waypoint>();
            SyncRouteIndices();
        }

        public void CollectFromChildren()
        {
            waypoints.Clear();
            for (int i = 0; i < transform.childCount; i++)
            {
                var wp = transform.GetChild(i).GetComponent<Waypoint>();
                if (wp != null) waypoints.Add(wp);
            }
            SyncRouteIndices();
        }

        public void SyncRouteIndices(bool refreshVisuals = true)
        {
            if (waypoints == null) return;
            for (int physical = 0; physical < waypoints.Count; physical++)
            {
                if (waypoints[physical] == null) continue;
                int logical = BoardNavigation.GetLogical(physical);
                var lane    = BoardNavigation.GetLane(physical);
                waypoints[physical].ApplyRouteData(
                    physical,
                    BoardNavigation.GetSquareType(logical, lane),
                    BoardNavigation.GetDisplayLabel(logical, lane),
                    BoardNavigation.GetEventId(logical, lane));
                if (refreshVisuals)
                    waypoints[physical].RequestVisualUpdate();
            }
        }

        public Bounds GetRouteBounds(float padding = 0.5f)
        {
            if (Count == 0) return new Bounds(transform.position, Vector3.one);

            var arr = new Waypoint[Count];
            for (int i = 0; i < Count; i++) arr[i] = waypoints[i];
            return BoardVisualUtility.CalculateWaypointBounds(arr, padding);
        }

        public void CenterRouteAtOrigin()
        {
            if (Count == 0) return;
            var bounds = GetRouteBounds(0f);
            var offset = -bounds.center;
            foreach (var wp in waypoints)
            {
                if (wp == null) continue;
                wp.transform.position += offset;
            }
        }

        private void OnValidate() => SyncRouteIndices(refreshVisuals: false);

        private void Start()
        {
            if (Application.isPlaying)
                SyncRouteIndices();
        }

        private void OnDrawGizmos()
        {
            if (!showRouteGizmos || waypoints == null || waypoints.Count < 2) return;

            Gizmos.color = new Color(0f, 1f, 1f, 0.9f);
            for (int i = 0; i < waypoints.Count - 1; i++)
            {
                if (waypoints[i] == null || waypoints[i + 1] == null) continue;
                Gizmos.DrawLine(waypoints[i].transform.position, waypoints[i + 1].transform.position);
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (waypoints == null) return;

            Gizmos.color = Color.yellow;
            foreach (var wp in waypoints)
            {
                if (wp == null) continue;
                Gizmos.DrawWireSphere(wp.transform.position, 0.22f);
            }

        }
    }
}
