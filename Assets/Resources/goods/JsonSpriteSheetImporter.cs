#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

[Serializable]
public class JsonSpriteSheet
{
    public string format;
    public string image;
    public string coordinateOrigin;
    public float pixelsPerUnit = 32f;
    public string filterMode = "Point";
    public string compression = "Uncompressed";
    public JsonSpriteRect[] sprites;
}

[Serializable]
public class JsonSpriteRect
{
    public string name;
    public float x;
    public float y;
    public float width;
    public float height;
    public JsonPivot pivot;
}

[Serializable]
public class JsonPivot
{
    public float x = 0.5f;
    public float y = 0.5f;
}

public static class JsonSpriteSheetImporter
{
    [MenuItem("Tools/Sprite Sheet/Apply Selected JSON")]
    public static void ApplySelectedJson()
    {
        TextAsset jsonAsset = Selection.activeObject as TextAsset;
        if (jsonAsset == null)
        {
            EditorUtility.DisplayDialog("Sprite Sheet Importer", "ProjectウィンドウでJSONファイルを1つ選択してください。", "OK");
            return;
        }

        string jsonPath = AssetDatabase.GetAssetPath(jsonAsset);
        JsonSpriteSheet sheet = JsonUtility.FromJson<JsonSpriteSheet>(jsonAsset.text);
        if (sheet == null || sheet.sprites == null || sheet.sprites.Length == 0)
        {
            EditorUtility.DisplayDialog("Sprite Sheet Importer", "JSON内にspritesがありません。", "OK");
            return;
        }

        string directory = Path.GetDirectoryName(jsonPath).Replace('\\', '/');
        string texturePath = string.IsNullOrEmpty(directory) ? sheet.image : directory + "/" + sheet.image;
        Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
        if (texture == null)
        {
            EditorUtility.DisplayDialog("Sprite Sheet Importer", "画像が見つかりません。JSONとPNGを同じフォルダに置いてください。\n" + texturePath, "OK");
            return;
        }

        TextureImporter importer = AssetImporter.GetAtPath(texturePath) as TextureImporter;
        if (importer == null)
        {
            EditorUtility.DisplayDialog("Sprite Sheet Importer", "TextureImporterを取得できませんでした。", "OK");
            return;
        }

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.spritePixelsPerUnit = Mathf.Max(1f, sheet.pixelsPerUnit);
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.filterMode = ParseFilterMode(sheet.filterMode);
        importer.textureCompression = ParseCompression(sheet.compression);

        int textureHeight = texture.height;
        SpriteMetaData[] metadata = new SpriteMetaData[sheet.sprites.Length];
        bool topLeft = string.Equals(sheet.coordinateOrigin, "top-left", StringComparison.OrdinalIgnoreCase);

        for (int i = 0; i < sheet.sprites.Length; i++)
        {
            JsonSpriteRect source = sheet.sprites[i];
            float unityY = topLeft ? textureHeight - source.y - source.height : source.y;
            JsonPivot sourcePivot = source.pivot ?? new JsonPivot();

            metadata[i] = new SpriteMetaData
            {
                name = string.IsNullOrWhiteSpace(source.name) ? "sprite_" + i.ToString("000") : source.name,
                rect = new Rect(source.x, unityY, source.width, source.height),
                alignment = (int)SpriteAlignment.Custom,
                pivot = new Vector2(sourcePivot.x, sourcePivot.y),
                border = Vector4.zero
            };
        }

#pragma warning disable 0618
        importer.spritesheet = metadata;
#pragma warning restore 0618
        importer.SaveAndReimport();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog(
            "Sprite Sheet Importer",
            sheet.sprites.Length + "個のSpriteを設定しました。\n" + texturePath,
            "OK"
        );
    }

    [MenuItem("Tools/Sprite Sheet/Apply Selected JSON", true)]
    private static bool ValidateApplySelectedJson()
    {
        string path = AssetDatabase.GetAssetPath(Selection.activeObject);
        return !string.IsNullOrEmpty(path) && path.EndsWith(".json", StringComparison.OrdinalIgnoreCase);
    }

    private static FilterMode ParseFilterMode(string value)
    {
        if (string.Equals(value, "Bilinear", StringComparison.OrdinalIgnoreCase)) return FilterMode.Bilinear;
        if (string.Equals(value, "Trilinear", StringComparison.OrdinalIgnoreCase)) return FilterMode.Trilinear;
        return FilterMode.Point;
    }

    private static TextureImporterCompression ParseCompression(string value)
    {
        if (string.Equals(value, "Compressed", StringComparison.OrdinalIgnoreCase)) return TextureImporterCompression.Compressed;
        if (string.Equals(value, "CompressedHQ", StringComparison.OrdinalIgnoreCase)) return TextureImporterCompression.CompressedHQ;
        if (string.Equals(value, "CompressedLQ", StringComparison.OrdinalIgnoreCase)) return TextureImporterCompression.CompressedLQ;
        return TextureImporterCompression.Uncompressed;
    }
}
#endif
