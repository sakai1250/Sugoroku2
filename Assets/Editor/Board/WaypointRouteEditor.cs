using UnityEditor;
using UnityEngine;
using Sugoroku.Board;

namespace Sugoroku.Editor.Board
{
    [CustomEditor(typeof(WaypointRoute))]
    public class WaypointRouteEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var route = (WaypointRoute)target;
            EditorGUILayout.Space(6);

            if (GUILayout.Button("子オブジェクトから収集（兄弟順）"))
            {
                Undo.RecordObject(route, "Collect Waypoints");
                route.CollectFromChildren();
                EditorUtility.SetDirty(route);
            }

            if (GUILayout.Button("ルートインデックスを再同期"))
            {
                Undo.RecordObject(route, "Sync Route Indices");
                route.SyncRouteIndices();
                EditorUtility.SetDirty(route);
            }

            if (GUILayout.Button("ルートを原点にセンタリング"))
            {
                Undo.RecordObject(route.transform, "Center Route");
                route.CenterRouteAtOrigin();
                EditorUtility.SetDirty(route);
            }

            EditorGUILayout.HelpBox(
                "シーンビュー: Route_Main を選択するとシアン色のルート線が表示されます。\n" +
                "マス名は W00, W01 … の短い名前のみ（草地タイルは生成しません）。",
                MessageType.Info);
        }

        private void OnSceneGUI()
        {
            var route = (WaypointRoute)target;
            if (route.Waypoints == null) return;

            Handles.color = Color.white;
            foreach (var wp in route.Waypoints)
            {
                if (wp == null) continue;
                Handles.Label(
                    wp.transform.position + Vector3.up * 0.4f,
                    $"{wp.RouteIndex}:{Waypoint.GetTypeShortLabel(wp.Type)}",
                    EditorStyles.miniLabel);
            }
        }
    }
}
