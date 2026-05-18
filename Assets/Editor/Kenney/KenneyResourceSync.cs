using System.IO;
using UnityEditor;
using UnityEngine;
using Sugoroku.Visual;
using Sugoroku.Editor;

namespace Sugoroku.Editor.Kenney
{
    public static class KenneyResourceSync
    {
        private const string ThirdPartyRoot = "Assets/ThirdParty/Kenney";
        private const string ResourcesRoot  = "Assets/Resources/ThirdParty/Kenney";

        public static void SyncAll()
        {
            CopyFolder($"{ThirdPartyRoot}/UIPack", $"{ResourcesRoot}/UIPack", "*.png");
            CopyFolder($"{ThirdPartyRoot}/GameIcons", $"{ResourcesRoot}/GameIcons", "*.png");
            CopyFolder($"{ThirdPartyRoot}/BoardgamePack/Pieces", $"{ResourcesRoot}/BoardgamePack/Pieces", "*.png");
            CopyFolder($"{ThirdPartyRoot}/BoardgamePack/Audio", $"{ResourcesRoot}/BoardgamePack/Audio", "*.ogg");
            CopyFolder($"{ThirdPartyRoot}/InterfaceSounds", $"{ResourcesRoot}/InterfaceSounds", "*.ogg");
            CopyFolder($"{ThirdPartyRoot}/BoardgamePack/Dice", $"{ResourcesRoot}/BoardgamePack/Dice", "*.png");
            CopyFolder($"{ThirdPartyRoot}/ToonCharacters", $"{ResourcesRoot}/ToonCharacters", "*.png");
            CopyFolder($"{ThirdPartyRoot}/Originalchar", $"{ResourcesRoot}/Originalchar", "*.png");

            AssetDatabase.Refresh();
            OriginalcharTextureImporter.ReimportAll();
            OriginalcharAssets.ClearCache();
            Debug.Log("✅ Kenney アセットを Resources に同期しました。" +
                        "\n  駒・立ち絵: Originalchar | UI: UIPack | アイコン: GameIcons | SE: InterfaceSounds / BoardgamePack");
        }

        public static void ApplyUiStyleToActiveScene()
        {
            var canvas = Object.FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                Debug.LogWarning("Canvas が見つかりません。");
                return;
            }

            SyncAll();
            Sugoroku.UI.KenneyUiStyler.StyleCanvas(canvas);
            Sugoroku.UI.KenneyUiStyler.EnsureDiceDisplay(canvas.transform);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
            Debug.Log("✅ Kenney UI スタイルをシーンに適用しました。");
        }

        private static void CopyFolder(string sourceDir, string destDir, string pattern)
        {
            if (!Directory.Exists(sourceDir)) return;

            Directory.CreateDirectory(destDir);
            foreach (var file in Directory.GetFiles(sourceDir, pattern))
            {
                if (file.EndsWith(".meta")) continue;
                var dest = Path.Combine(destDir, Path.GetFileName(file));
                File.Copy(file, dest, overwrite: true);
            }
        }
    }
}
