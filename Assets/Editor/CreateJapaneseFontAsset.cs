using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.TextCore;
using UnityEngine.TextCore.LowLevel;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;

namespace Sugoroku.Editor
{
    /// <summary>
    /// NotoSansJP の TTF から Dynamic TMP Font Asset を生成する。
    /// 表示時に必要な文字をアトラスへ動的に追加する（Unity 2021+ TMP 推奨方式）。
    /// </summary>
    public static class CreateJapaneseFontAsset
    {
        public const string SavePath = "Assets/Fonts/NotoSansJP-Regular SDF.asset";
        public const string RuntimeSavePath = "Assets/Resources/Fonts/NotoSansJP-Regular SDF.asset";
        private const string LegacyRuntimeSavePath = "Assets/Resources/Fonts & Materials/NotoSansJP-Regular SDF.asset";
        private const string PreferredTtfPath = "Assets/Fonts/NotoSansJP-Regular.ttf";

        private const int SamplingPointSize = 90;
        private const int AtlasPadding = 9;
        private const int AtlasSize = 2048;

        private const string FullCharsetAssetPath = "Assets/Resources/Fonts/JapaneseFullCharset.txt";

        /// <summary>フルセットのテキストアセットが見つからない場合のフォールバック。</summary>
        private const string FallbackPreWarmCharacters =
            "あいうえおかきくけこさしすせそたちつてとなにぬねのはひふへほまみむめもやゆよらりるれろわをん" +
            "がぎぐげござじずぜぞだぢづでどばびぶべぼぱぴぷぺぽゃゅょっー、。！？（）・△×" +
            "アイウエオカキクケコサシスセソタチツテトナニヌネノハヒフヘホマミムメモヤユヨラリルレロワヲン" +
            "ガギグゲゴザジズゼゾダヂヅデドバビブベボパピプペポャュョッー" +
            "大学院生すごろくゲームスタート設定実績修了発表脱落所持金万メンタル徳選択中コマキャラクター" +
            "多趣味系ゴール学費進捗残りマス行動待ちターン開始終了ダイスを振るワザ回避休み破産失踪計算" +
            "イベントタイトルイベント説明文タイトルへポーズ再開閉じる人間未実装" +
            "【】生活未納強制退学通知書届研究室机赤叩節約重要音信不通ドロップアウト消息明薄暗部屋" +
            "画面虚光着拒否読嵐教授無視留年業績不振除籍対象者一覧自分番号載総合スコア暫定順位全員" +
            "初期ステータス固有特性戦略的役割低円位博士進職候補修羅道";

        private static string PreWarmCharacters
        {
            get
            {
                var charsetAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(FullCharsetAssetPath);
                return charsetAsset != null && !string.IsNullOrEmpty(charsetAsset.text)
                    ? charsetAsset.text
                    : FallbackPreWarmCharacters;
            }
        }

        [MenuItem("Tools/Sugoroku/Regenerate Japanese Font Asset")]
        public static void RegenerateFromMenu() => Create();

        [MenuItem("Tools/Sugoroku/Apply Japanese Font To All Scenes")]
        public static void ApplyFontToAllScenesMenu() => ApplyFontToAllScenes(LoadOrCreateFontAsset());

        /// <summary>既存アセットを Dynamic に切り替え、よく使う文字をプリウォームする。</summary>
        [MenuItem("Tools/Sugoroku/Set Japanese Font To Dynamic Mode")]
        public static void SetExistingFontToDynamicMode()
        {
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(SavePath);
            if (font == null)
            {
                EditorUtility.DisplayDialog("フォントなし", "先に Regenerate を実行してください。", "OK");
                return;
            }

            font.atlasPopulationMode = AtlasPopulationMode.Dynamic;
            DisableClearDynamicDataOnBuild(font);
            EnsureDistanceFieldMaterial(font);

            if (!HasBakedGlyphs(font))
                RepairAndPrewarm(font);

            EditorUtility.SetDirty(font);
            AssetDatabase.SaveAssets();
            Debug.Log($"✅ {SavePath} を Dynamic モードに設定しました（グリフ {CountBakedGlyphs(font)} 件）。");
        }

        [MenuItem("Tools/Sugoroku/Fix Japanese Font Import (Remove Duplicates)")]
        public static void FixImportFromMenu()
        {
            RemoveBrokenFontAssets();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            Debug.Log("✅ 破損フォントを削除しました。続けて「Regenerate Japanese Font Asset」を実行してください。");
        }

        public static void Create()
        {
            Font ttf = FindNotoSansJpFont();
            if (ttf == null)
            {
                EditorUtility.DisplayDialog(
                    "TTF が見つかりません",
                    "NotoSansJP-Regular.ttf を Assets/Fonts/ に配置してください。",
                    "OK");
                return;
            }

            if (!TryBuildDynamicFontAsset(ttf, "NotoSansJP-Regular SDF", out TMP_FontAsset built, out string error))
            {
                Debug.LogError("Dynamic フォントの生成に失敗しました。 " + error);
                return;
            }

            DeleteAssetIfExists(LegacyRuntimeSavePath);
            DeleteAssetIfExists(RuntimeSavePath);
            DeleteAssetIfExists(SavePath);

            string dir = Path.GetDirectoryName(SavePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            AssetDatabase.CreateAsset(built, SavePath);
            EnsureMaterialAndAtlasSubAssets(built);
            DisableClearDynamicDataOnBuild(built);
            SetSourceFontGuid(built, ttf);

            EditorUtility.SetDirty(built);
            AssetDatabase.SaveAssets();

            built = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(SavePath);
            if (built == null || !HasBakedGlyphs(built))
            {
                Debug.LogError("フォントアセットの保存後の検証に失敗しました。");
                return;
            }

            MirrorRuntimeAsset();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            Debug.Log(
                $"✅ Dynamic 日本語フォントを生成しました（プリウォーム {CountBakedGlyphs(built)} グリフ）: {SavePath}\n" +
                "Inspector の Atlas Population Mode = Dynamic です。不足文字は実行時に自動追加されます。");
            ApplyFontToAllScenes(built);
        }

        public static TMP_FontAsset LoadOrCreateFontAsset()
        {
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(SavePath);
            if (IsUsableFont(font)) return font;
            font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(RuntimeSavePath);
            if (IsUsableFont(font)) return font;
            return null;
        }

        public static bool HasBakedGlyphs(TMP_FontAsset font)
        {
            if (font?.glyphTable == null || font.glyphTable.Count == 0)
                return false;

            foreach (var glyph in font.glyphTable)
            {
                if (glyph != null && glyph.glyphRect.width > 0 && glyph.glyphRect.height > 0)
                    return true;
            }

            return false;
        }

        private static bool IsUsableFont(TMP_FontAsset font)
        {
            if (font == null) return false;
            if (font.atlasPopulationMode == AtlasPopulationMode.Dynamic)
                return font.sourceFontFile != null || HasBakedGlyphs(font);
            return HasBakedGlyphs(font);
        }

        private static bool TryBuildDynamicFontAsset(
            Font sourceFont,
            string assetName,
            out TMP_FontAsset fontAsset,
            out string error)
        {
            fontAsset = null;
            error = null;

            if (sourceFont == null)
            {
                error = "ソースフォントが null です。";
                return false;
            }

            if (TMP_Settings.instance == null)
            {
                error = "TMP Essential Resources が未インポートです。";
                return false;
            }

            FontEngineError init = FontEngine.InitializeFontEngine();
            if (init != FontEngineError.Success)
            {
                error = $"FontEngine 初期化失敗: {init}";
                return false;
            }

            FontEngineError load = FontEngine.LoadFontFace(sourceFont, SamplingPointSize);
            if (load != FontEngineError.Success)
            {
                error =
                    $"フォントフェース読み込み失敗 ({load})。NotoSansJP-Regular.ttf の Import Settings で " +
                    "\"Include Font Data\" を有効にしてください。";
                return false;
            }

            fontAsset = TMP_FontAsset.CreateFontAsset(
                sourceFont,
                SamplingPointSize,
                AtlasPadding,
                GlyphRenderMode.SDFAA,
                AtlasSize,
                AtlasSize,
                AtlasPopulationMode.Dynamic,
                true);

            if (fontAsset == null)
            {
                error = "TMP_FontAsset.CreateFontAsset に失敗しました。";
                return false;
            }

            fontAsset.name = assetName;
            SetSourceFontGuid(fontAsset, sourceFont);
            fontAsset.atlasPopulationMode = AtlasPopulationMode.Dynamic;

            EnsureDistanceFieldMaterial(fontAsset);

            if (fontAsset.material != null)
                fontAsset.material.name = assetName + " Material";
            if (fontAsset.atlasTexture != null)
                fontAsset.atlasTexture.name = assetName + " Atlas";

            RepairAndPrewarm(fontAsset);

            if (!HasBakedGlyphs(fontAsset))
            {
                error = "プリウォーム後もグリフが空です。TTF が Variable Font になっていないか確認してください。";
                Object.DestroyImmediate(fontAsset);
                fontAsset = null;
                return false;
            }

            return true;
        }

        /// <summary>幅 0 の壊れたグリフを消してから Dynamic で文字を追加する。</summary>
        private static void RepairAndPrewarm(TMP_FontAsset fontAsset)
        {
            if (fontAsset == null) return;

            if (HasBrokenZeroSizeGlyphs(fontAsset))
            {
                typeof(TMP_FontAsset).GetMethod(
                        "UpdateFontAssetData",
                        BindingFlags.Instance | BindingFlags.NonPublic)
                    ?.Invoke(fontAsset, null);
            }

            fontAsset.TryAddCharacters(PreWarmCharacters, out string missing);
            fontAsset.ReadFontAssetDefinition();
            EnsureDistanceFieldMaterial(fontAsset);

            if (!string.IsNullOrEmpty(missing))
                Debug.LogWarning($"プリウォームできなかった文字 ({missing.Length} 字): {missing}");
        }

        private static bool HasBrokenZeroSizeGlyphs(TMP_FontAsset font)
        {
            if (font?.glyphTable == null) return false;
            foreach (var g in font.glyphTable)
            {
                if (g != null && g.glyphRect.width == 0 && g.glyphRect.height == 0)
                    return true;
            }
            return false;
        }

        private static void EnsureDistanceFieldMaterial(TMP_FontAsset fontAsset)
        {
            if (fontAsset == null) return;

            Shader shader = Shader.Find("TextMeshPro/Distance Field")
                            ?? Shader.Find("TextMeshPro/Mobile/Distance Field");
            if (shader == null) return;

            if (fontAsset.material == null || fontAsset.material.shader != shader)
            {
                var mat = new Material(shader);
                fontAsset.material = mat;
            }

            var atlas = fontAsset.atlasTexture;
            if (atlas == null) return;

            fontAsset.material.SetTexture(ShaderUtilities.ID_MainTex, atlas);
            fontAsset.material.SetFloat(ShaderUtilities.ID_TextureWidth, fontAsset.atlasWidth);
            fontAsset.material.SetFloat(ShaderUtilities.ID_TextureHeight, fontAsset.atlasHeight);
            if (fontAsset.material.HasProperty(ShaderUtilities.ID_GradientScale))
                fontAsset.material.SetFloat(ShaderUtilities.ID_GradientScale, fontAsset.atlasPadding + 1);
        }

        private static int CountBakedGlyphs(TMP_FontAsset font)
        {
            if (font?.glyphTable == null) return 0;
            int count = 0;
            foreach (var glyph in font.glyphTable)
            {
                if (glyph != null && glyph.glyphRect.width > 0 && glyph.glyphRect.height > 0)
                    count++;
            }
            return count;
        }

        private static void SetSourceFontGuid(TMP_FontAsset fontAsset, Font sourceFont)
        {
            string guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(sourceFont));
            SetInternalField(fontAsset, "m_SourceFontFileGUID", guid);
            SetInternalField(fontAsset, "m_SourceFontFile_EditorRef", sourceFont);
        }

        private static void SetInternalField(TMP_FontAsset fontAsset, string fieldName, object value)
        {
            typeof(TMP_FontAsset).GetField(
                    fieldName,
                    BindingFlags.Instance | BindingFlags.NonPublic)
                ?.SetValue(fontAsset, value);
        }

        private static void RemoveBrokenFontAssets()
        {
            DeleteAssetIfExists(LegacyRuntimeSavePath);
            DeleteAssetIfExists(RuntimeSavePath);

            var main = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(SavePath);
            if (main != null && !IsUsableFont(main))
                DeleteAssetIfExists(SavePath);
        }

        private static void DeleteAssetIfExists(string path)
        {
            if (AssetDatabase.LoadAssetAtPath<Object>(path) != null)
                AssetDatabase.DeleteAsset(path);
        }

        private static void MirrorRuntimeAsset()
        {
            var source = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(SavePath);
            if (!IsUsableFont(source)) return;

            string dir = Path.GetDirectoryName(RuntimeSavePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            DeleteAssetIfExists(RuntimeSavePath);

            if (!AssetDatabase.CopyAsset(SavePath, RuntimeSavePath))
                Debug.LogWarning($"Resources へのコピーに失敗: {RuntimeSavePath}");
        }

        private static void EnsureMaterialAndAtlasSubAssets(TMP_FontAsset fontAsset)
        {
            if (fontAsset == null) return;

            if (fontAsset.atlasTextures != null)
            {
                foreach (var tex in fontAsset.atlasTextures)
                {
                    if (tex == null) continue;
                    if (!AssetDatabase.IsSubAsset(tex))
                        AssetDatabase.AddObjectToAsset(tex, fontAsset);
                }
            }

            if (fontAsset.material != null && !AssetDatabase.IsSubAsset(fontAsset.material))
                AssetDatabase.AddObjectToAsset(fontAsset.material, fontAsset);
        }

        private static void DisableClearDynamicDataOnBuild(TMP_FontAsset fontAsset)
        {
            if (fontAsset == null) return;
            SetInternalField(fontAsset, "m_ClearDynamicDataOnBuild", false);
        }

        private static void ApplyFontToAllScenes(TMP_FontAsset font)
        {
            if (font == null)
            {
                Debug.LogWarning("適用するフォントがありません。先に Regenerate を実行してください。");
                return;
            }

            int count = 0;
            foreach (var scenePath in EditorBuildSettings.scenes)
            {
                if (string.IsNullOrEmpty(scenePath.path)) continue;
                var scene = EditorSceneManager.OpenScene(scenePath.path, OpenSceneMode.Single);
                foreach (var tmp in Object.FindObjectsByType<TextMeshProUGUI>(FindObjectsSortMode.None))
                {
                    tmp.font = font;
                    EditorUtility.SetDirty(tmp);
                    count++;
                }
                EditorSceneManager.SaveScene(scene);
            }
            Debug.Log($"✅ ビルド設定内シーン {count} 個の TextMeshProUGUI にフォントを適用しました。");
        }

        public static Font FindNotoSansJpFont()
        {
            var preferred = AssetDatabase.LoadAssetAtPath<Font>(PreferredTtfPath);
            if (preferred != null) return preferred;

            foreach (var query in new[] { "NotoSansJP-Regular t:Font", "NotoSansJP t:Font" })
            {
                foreach (var g in AssetDatabase.FindAssets(query))
                {
                    var path = AssetDatabase.GUIDToAssetPath(g);
                    if (path.Contains("VariableFont")) continue;
                    var f = AssetDatabase.LoadAssetAtPath<Font>(path);
                    if (f != null) return f;
                }
            }
            return null;
        }
    }
}
