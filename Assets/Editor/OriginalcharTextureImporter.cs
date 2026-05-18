using System.IO;
using UnityEditor;
using UnityEngine;

namespace Sugoroku.Editor
{
    /// <summary>Originalchar の PNG を Sprite としてインポートする。</summary>
    public static class OriginalcharTextureImporter
    {
        private static readonly string[] Folders =
        {
            "Assets/ThirdParty/Kenney/Originalchar",
            "Assets/Resources/ThirdParty/Kenney/Originalchar",
            "Assets/ThirdParty/Kenney/BoardgamePack/Dice",
            "Assets/Resources/ThirdParty/Kenney/BoardgamePack/Dice",
            "Assets/ThirdParty/Kenney/GameIcons",
            "Assets/Resources/ThirdParty/Kenney/GameIcons",
        };

        [InitializeOnLoadMethod]
        private static void ReimportOnLoad()
        {
            // 初回のみ：spriteMode が Default のままの画像を修正
            if (SessionState.GetBool("Sugoroku_OriginalcharFixed", false)) return;
            if (ReimportAll())
                SessionState.SetBool("Sugoroku_OriginalcharFixed", true);
        }

        public static bool ReimportAll()
        {
            bool changed = false;
            foreach (var folder in Folders)
            {
                if (!Directory.Exists(folder)) continue;
                foreach (var file in Directory.GetFiles(folder, "*.png"))
                    changed |= ApplySpriteImport(file);
            }

            if (changed)
            {
                AssetDatabase.Refresh();
                Sugoroku.Visual.OriginalcharAssets.ClearCache();
            }

            return changed;
        }

        private static bool ApplySpriteImport(string assetPath)
        {
            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null) return false;

            if (importer.textureType == TextureImporterType.Sprite &&
                importer.spriteImportMode == SpriteImportMode.Single)
                return false;

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
            return true;
        }
    }
}
