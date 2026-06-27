using System.IO;
using UnityEditor;
using UnityEngine;

namespace Sugoroku.Editor
{
    /// <summary>Resources/event-MASU の PNG を Sprite としてインポートする。</summary>
    public static class EventMasuTextureImporter
    {
        private const string Folder = "Assets/Resources/event-MASU";

        [InitializeOnLoadMethod]
        private static void ReimportOnLoad()
        {
            if (SessionState.GetBool("Sugoroku_EventMasuFixed", false)) return;
            if (ReimportAll())
                SessionState.SetBool("Sugoroku_EventMasuFixed", true);
        }

        [MenuItem("Tools/Sugoroku/Import Event-MASU Textures As Sprites")]
        public static bool ReimportAll()
        {
            if (!Directory.Exists(Folder)) return false;

            bool changed = false;
            foreach (var file in Directory.GetFiles(Folder))
            {
                var ext = Path.GetExtension(file).ToLowerInvariant();
                if (ext is not ".png") continue;
                changed |= ApplySpriteImport(file.Replace('\\', '/'));
            }

            if (changed)
            {
                AssetDatabase.Refresh();
                Sugoroku.Board.EventMasuArt.ClearCache();
                Debug.Log("✅ event-MASU テクスチャを Sprite として再インポートしました。");
            }

            return changed;
        }

        private static bool ApplySpriteImport(string assetPath)
        {
            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null) return false;

            bool needsUpdate =
                importer.textureType != TextureImporterType.Sprite ||
                importer.spriteImportMode != SpriteImportMode.Single;

            if (!needsUpdate) return false;

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
