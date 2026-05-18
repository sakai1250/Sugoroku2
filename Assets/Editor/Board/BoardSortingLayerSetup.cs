using UnityEditor;
using UnityEngine;

namespace Sugoroku.Editor.Board
{
    public static class BoardSortingLayerSetup
    {
        private static readonly string[] RequiredLayers = { "Background", "Board", "Player" };

        public static void EnsureSortingLayers()
        {
            var tagManager = AssetDatabase.LoadMainAssetAtPath("ProjectSettings/TagManager.asset");
            if (tagManager == null)
            {
                Debug.LogError("TagManager.asset が見つかりません。");
                return;
            }

            var so = new SerializedObject(tagManager);
            var layers = so.FindProperty("m_SortingLayers");
            if (layers == null)
            {
                Debug.LogError("m_SortingLayers が見つかりません。");
                return;
            }

            int added = 0;
            foreach (var layerName in RequiredLayers)
            {
                if (HasLayer(layers, layerName)) continue;

                layers.InsertArrayElementAtIndex(layers.arraySize);
                var entry = layers.GetArrayElementAtIndex(layers.arraySize - 1);
                entry.FindPropertyRelative("name").stringValue     = layerName;
                entry.FindPropertyRelative("uniqueID").intValue    = GenerateUniqueId(layerName);
                entry.FindPropertyRelative("locked").boolValue   = false;
                added++;
            }

            if (added > 0)
            {
                so.ApplyModifiedProperties();
                AssetDatabase.SaveAssets();
                Debug.Log($"✅ Sorting Layers を追加しました: {string.Join(", ", RequiredLayers)}（{added} 件）");
            }
            else
                Debug.Log("Sorting Layers は既に設定済みです。");
        }

        private static bool HasLayer(SerializedProperty layers, string name)
        {
            for (int i = 0; i < layers.arraySize; i++)
            {
                if (layers.GetArrayElementAtIndex(i).FindPropertyRelative("name").stringValue == name)
                    return true;
            }
            return false;
        }

        private static int GenerateUniqueId(string name) =>
            name.GetHashCode() & 0x7FFFFFFF;
    }
}
