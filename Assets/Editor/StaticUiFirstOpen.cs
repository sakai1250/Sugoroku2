using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Sugoroku.Editor
{
    /// <summary>
    /// The distributed archive contains pre-existing scenes as well as a new static
    /// authoring pass. Run the pass once, after the Unity Editor finishes importing
    /// packages and scripts. A stamp prevents re-baking on subsequent launches.
    /// </summary>
    [InitializeOnLoad]
    internal static class StaticUiFirstOpen
    {
        private const string Stamp = "ProjectSettings/StaticUiAuthoring.complete";
        private static bool attempted;

        static StaticUiFirstOpen()
        {
            EditorApplication.update += Check;
        }

        private static void Check()
        {
            if (attempted || File.Exists(Stamp))
            {
                EditorApplication.update -= Check;
                return;
            }
            if (EditorApplication.isPlayingOrWillChangePlaymode ||
                EditorApplication.isCompiling || EditorApplication.isUpdating)
                return;
            if (!AssetDatabase.IsValidFolder("Assets/Scenes")) return;

            attempted = true;
            EditorApplication.update -= Check;
            try
            {
                BakeStaticUiScenes.BakeAll();
                // Do not mark success unless every target scene was serialized.
                var guidPath = "Assets/Scripts/UI/StaticUiAuthoring.cs.meta";
                string meta = File.ReadAllText(guidPath);
                var m = System.Text.RegularExpressions.Regex.Match(meta, @"guid:\s*([0-9a-f]{32})");
                if (!m.Success) throw new IOException("Static UI script guid not found");
                string[] scenes = { "TitleScene", "CharacterSelectScene", "GameUIScene", "ResultScene", "GameOverScene" };
                foreach (string name in scenes)
                {
                    string path = "Assets/Scenes/" + name + ".unity";
                    if (!File.ReadAllText(path).Contains(m.Groups[1].Value))
                        throw new IOException("Baked marker not serialized: " + path);
                }
                File.WriteAllText(Stamp, "Static UI baked once. Delete this file only if you want first-open bake to run again.\n");
                Debug.Log("[Static UI] First-open conversion completed. Edit the 5 UI scenes directly.");
            }
            catch (Exception e)
            {
                Debug.LogError("[Static UI] First-open conversion was not completed. Run Tools/UI/1. 全画面を静的化してSceneに保存 after fixing this error.\n" + e);
            }
        }
    }
}
