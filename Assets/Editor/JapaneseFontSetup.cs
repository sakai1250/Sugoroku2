using UnityEngine;
using UnityEditor;
using TMPro;

namespace Sugoroku.Editor
{
    /// <summary>
    /// 日本語フォントのセットアップ補助。
    /// NotoSansJP が未インポートの場合に警告を出す。
    /// </summary>
    public static class JapaneseFontSetup
    {
        public static void CheckFont()
        {
            var font = FindJapaneseFont();
            if (font != null)
            {
                Debug.Log($"✅ 日本語フォント検出: {font.name}");
                return;
            }

            Debug.LogWarning(
                "⚠️ 日本語フォントが見つかりません。\n" +
                "推奨: NotoSansJP-Regular.ttf を Assets/Fonts/ にインポートし、\n" +
                "Window > TextMeshPro > Font Asset Creator で SDF フォントを生成してください。\n" +
                "フォント名は「NotoSansJP-Regular SDF」として Assets/Resources/Fonts & Materials/ に保存してください。"
            );

            // フォントが無い場合はデフォルト（LiberationSans）のフォールバックに
            // 日本語グリフを追加しようとしても無意味なため、ユーザーにインポートを促す
            EditorUtility.DisplayDialog(
                "日本語フォント未設定",
                "日本語フォントが見つかりません。\n\n" +
                "1. NotoSansJP-Regular.ttf を Assets/Fonts/ にインポート\n" +
                "2. Window > TextMeshPro > Font Asset Creator を開く\n" +
                "3. Source Font: NotoSansJP-Regular を選択\n" +
                "4. Character Set: Unicode Range (Hex) に Unicode範囲を入力\n" +
                "5. Generate Font Atlas → Save As\n" +
                "6. 保存先: Assets/Resources/Fonts & Materials/NotoSansJP-Regular SDF\n\n" +
                "または Window > TextMeshPro > Import TMP Essential Resources から\n" +
                "SourceHanSans等の日本語対応フォントをインポートしてください。",
                "OK"
            );
        }

        public static void ApplyFontToAllTMP(bool silent = false)
        {
            var font = FindJapaneseFont();
            if (font == null)
            {
                if (silent)
                    Debug.LogWarning("日本語フォント未設定のため TMP への適用をスキップしました。");
                else
                    CheckFont();
                return;
            }

            int count = 0;
            foreach (var tmp in Object.FindObjectsByType<TextMeshProUGUI>(FindObjectsSortMode.None))
            {
                tmp.font = font;
                EditorUtility.SetDirty(tmp);
                count++;
            }
            Debug.Log($"✅ {count} 個の TextMeshProUGUI に {font.name} を適用しました。");
        }

        public static TMP_FontAsset FindJapaneseFont()
        {
            foreach (var path in new[]
            {
                "Assets/Fonts/NotoSansJP-Regular SDF.asset",
                "Assets/Resources/Fonts/NotoSansJP-Regular SDF.asset",
                "Assets/Resources/Fonts & Materials/NotoSansJP-Regular SDF.asset"
            })
            {
                var preferred = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
                if (preferred != null) return preferred;
            }

            string[] guids = AssetDatabase.FindAssets("t:TMP_FontAsset NotoSansJP");
            foreach (var g in guids)
            {
                var f = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetDatabase.GUIDToAssetPath(g));
                if (f != null) return f;
            }
            guids = AssetDatabase.FindAssets("t:TMP_FontAsset NotoSansCJK");
            foreach (var g in guids)
            {
                var f = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetDatabase.GUIDToAssetPath(g));
                if (f != null) return f;
            }
            guids = AssetDatabase.FindAssets("t:TMP_FontAsset SourceHanSans");
            foreach (var g in guids)
            {
                var f = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetDatabase.GUIDToAssetPath(g));
                if (f != null) return f;
            }
            return null;
        }
    }
}
