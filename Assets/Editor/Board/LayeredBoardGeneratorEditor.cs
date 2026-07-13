using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Sugoroku.Board;

namespace Sugoroku.Editor.Board
{
    [CustomEditor(typeof(LayeredBoardGenerator))]
    public class LayeredBoardGeneratorEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var gen = (LayeredBoardGenerator)target;
            EditorGUILayout.Space(8);

            if (GUILayout.Button("Setup Sorting Layers + Waypoint Prefab", GUILayout.Height(24)))
                WaypointPrefabBuilder.SetupProject();

            if (GUILayout.Button("Rebuild Clean Board（推奨）", GUILayout.Height(32)))
                RebuildSnakeBoardInScene();

            if (GUILayout.Button("Clear Layered Board"))
            {
                gen.ClearLayeredBoard();
                MarkSceneDirty();
            }

            EditorGUILayout.HelpBox(
                "「Rebuild Clean Board」で S字ルート 20 マスをカード型 UI で再配置します。\n" +
                "イベントマスは EventId から events.json のタイトルを表示。タップで詳細。\n" +
                "背景は薄く表示（敷物）。完成後に BackgroundArt を合わせてください。",
                MessageType.Info);
        }

        public static void RebuildSnakeBoardInScene()
        {
            WaypointPrefabBuilder.SetupProject();
            var board = FindBoard();
            if (board == null)
            {
                Debug.LogWarning("シーン内に Board が見つかりません。");
                return;
            }

            var gen = board.GetComponent<LayeredBoardGenerator>();
            if (gen == null)
                gen = board.gameObject.AddComponent<LayeredBoardGenerator>();

            RebuildClean(gen);
        }

        public static void CleanHierarchy()
        {
            var board = FindBoard();
            if (board == null) return;

            var gen = board.GetComponent<LayeredBoardGenerator>();
            gen?.RemoveStrayBoardObjects();

            foreach (var name in new[] { "LayeredBoard", "Route_Main", "SafeDioramaBoard", "HexagonKitField" })
            {
                var t = board.transform.Find(name);
                if (t != null) DestroyImmediate(t.gameObject);
            }

            RemoveLabelMeshesUnder(board);
            MarkSceneDirty();
            Debug.Log("Board 配下をクリーンアップしました。");
        }

        private static void RebuildClean(LayeredBoardGenerator gen)
        {
            EnsurePrefabAssigned(gen);
            gen.RemoveStrayBoardObjects();
            RemoveLabelMeshesUnder(gen.transform.parent != null ? gen.transform.parent : gen.transform);
            gen.ClearLayeredBoard();
            gen.GenerateLayeredBoard();
            MarkSceneDirty();
            Debug.Log("✅ S字ルート 20 マスを再生成しました。Route_Main を選択して進行線を確認してください。");
        }

        private static void RemoveLabelMeshesUnder(Transform root)
        {
            foreach (var tmp in root.GetComponentsInChildren<TMPro.TextMeshPro>(true))
            {
                if (tmp.GetComponentInParent<Waypoint>() != null)
                    DestroyImmediate(tmp.gameObject);
            }
            foreach (var tm in root.GetComponentsInChildren<TextMesh>(true))
            {
                if (tm.GetComponentInParent<Waypoint>() != null)
                    DestroyImmediate(tm.gameObject);
            }
        }

        private static void EnsurePrefabAssigned(LayeredBoardGenerator gen)
        {
            var so = new SerializedObject(gen);
            var prop = so.FindProperty("_waypointPrefab");
            if (prop != null && prop.objectReferenceValue == null)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<Waypoint>(WaypointPrefabBuilder.MassTextCardPrefabPath)
                    ?? AssetDatabase.LoadAssetAtPath<Waypoint>(WaypointPrefabBuilder.PrefabPath);
                if (prefab != null)
                {
                    prop.objectReferenceValue = prefab;
                    so.ApplyModifiedPropertiesWithoutUndo();
                }
            }

            var dim = so.FindProperty("_dimBackgroundArt");
            if (dim != null && !dim.boolValue)
            {
                dim.boolValue = true;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static Transform FindBoard()
        {
            if (Selection.activeGameObject != null &&
                Selection.activeGameObject.name == "Board")
                return Selection.activeGameObject.transform;

            var bm = Object.FindFirstObjectByType<BoardManager>();
            return bm != null ? bm.transform : null;
        }

        private static void MarkSceneDirty() =>
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
    }
}
