using System.Reflection;
using TMPro;
using UnityEngine;

namespace Sugoroku.UI
{
    /// <summary>日本語 TMP フォントをランタイムで確保（SDF 未生成時は TTF から Dynamic 生成）。</summary>
    public static class JapaneseFontProvider
    {
        private static TMP_FontAsset _cached;
        private static bool _warned;
        private static MethodInfo _updateFontAssetDataMethod;
        private static bool _updateFontAssetDataMethodResolved;

        private const int SamplingPointSize = 90;
        private const int AtlasPadding = 9;
        private const int AtlasSize = 2048;
        private const int GlyphRenderModeSdfAa = 3;
        private const int AtlasPopulationDynamic = 1;

        private const string SdfResourcePath = "Fonts/NotoSansJP-Regular SDF";
        private const string TtfResourcePath = "Fonts/NotoSansJP-Regular";
        private const string RuntimeFontName = "NotoSansJP Runtime Dynamic SDF";
        private const string FullCharsetResourcePath = "Fonts/JapaneseFullCharset";

        /// <summary>ゲーム内で最低限必要な文字（フルセットのリソースが読めない場合のフォールバック）。</summary>
        private const string FallbackPreWarmCharacters =
            "あいうえおかきくけこさしすせそたちつてとなにぬねのはひふへほまみむめもやゆよらりるれろわをん" +
            "がぎぐげござじずぜぞだぢづでどばびぶべぼぱぴぷぺぽゃゅょっー、。！？（）・△×" +
            "★◇♪" +
            "アイウエオカキクケコサシスセソタチツテトナニヌネノハヒフヘホマミムメモヤユヨラリルレロワヲン" +
            "ガギグゲゴザジズゼゾダヂヅデドバビブベボパピプペポャュョッー" +
            "大学院生すごろくゲームスタート設定実績修了発表脱落所持金万メンタル徳選択中コマキャラクター" +
            "多趣味系ゴール学費進捗残りマス行動待ちターン開始終了ダイスを振るワザ回避休み破産失踪計算" +
            "イベントタイトルイベント説明文タイトルへポーズ再開閉じる人間未実装選択肢条件を満たしていません" +
            "【】生活未納強制退学通知書届研究室机赤叩節約重要音信不通ドロップアウト消息明薄暗部屋" +
            "画面虚光着拒否読嵐教授無視留年業績不振除籍対象者一覧自分番号載総合スコア暫定順位全員" +
            "初期ステータス固有特性戦略的役割低円位博士進職候補修羅道" +
            "✓・？？？（）◎相性";

        private static string _preWarmCharacters;

        private static string PreWarmCharacters
        {
            get
            {
                if (_preWarmCharacters == null)
                {
                    var charsetAsset = Resources.Load<TextAsset>(FullCharsetResourcePath);
                    _preWarmCharacters = charsetAsset != null && !string.IsNullOrEmpty(charsetAsset.text)
                        ? charsetAsset.text
                        : FallbackPreWarmCharacters;
                }
                return _preWarmCharacters;
            }
        }

        /// <summary>
        /// キャッシュ済みフォント・プリウォーム文字列・リフレクション結果を破棄する。
        /// Domain Reload が無効な環境で Play ボタンを押すたびに最新のフォント/文字セットを
        /// 反映させたい場合に、エディタ側から呼び出す想定。
        /// </summary>
        public static void ResetCache()
        {
            _cached = null;
            _warned = false;
            _preWarmCharacters = null;
            _updateFontAssetDataMethod = null;
            _updateFontAssetDataMethodResolved = false;
        }

        public static void WarmUp() => WarmUpAndSetDefault();

        public static void WarmUpAndSetDefault()
        {
            var font = Get();
            SetAsTmpDefault(font);
        }

        public static TMP_FontAsset Get()
        {
            if (_cached != null && IsUsableJapaneseFont(_cached))
                return _cached;

            _cached = null;

            var sdf = Resources.Load<TMP_FontAsset>(SdfResourcePath);
            if (sdf != null)
            {
                // 実行時に文字を追加する Dynamic モードのフォントは、共有アセットを直接書き換えると
                // エディタ再生中に「Importer generated inconsistent result」の再インポート警告が
                // 発生するため、ランタイム専用のインスタンスへ複製してから使用する。
                var runtimeInstance = sdf.atlasPopulationMode == AtlasPopulationMode.Dynamic
                    ? CreateRuntimeInstance(sdf)
                    : sdf;

                if (runtimeInstance != null && TryEnsureBaked(runtimeInstance))
                {
                    _cached = runtimeInstance;
                    return _cached;
                }
            }

            _cached = CreateDynamicFromTtf();
            if (_cached != null)
                TryEnsureBaked(_cached);

            if (!IsUsableJapaneseFont(_cached))
                _cached = null;

            if (_cached == null && !_warned)
            {
                Debug.LogWarning(
                    "日本語フォントを読み込めません。Unity で Tools → Sugoroku → Regenerate Japanese Font Asset を実行してください。");
                _warned = true;
            }

            return _cached;
        }

        /// <summary>常に有効な日本語フォントへ差し替え（アトラス空の Noto 参照を修復）。</summary>
        public static void Apply(TextMeshProUGUI tmp)
        {
            if (tmp == null) return;

            var font = Get();
            if (font == null) return;

            if (!string.IsNullOrEmpty(tmp.text))
                font = EnsureCharactersForText(font, tmp.text);

            if (font == null) return;

            tmp.font = font;
            tmp.ForceMeshUpdate(true);
        }

        public static bool NeedsJapaneseFont(TMP_FontAsset current)
        {
            if (current == null) return true;
            return IsNonJapaneseFallbackFont(current);
        }

        private static bool IsNonJapaneseFallbackFont(TMP_FontAsset font)
        {
            string n = font.name ?? "";
            return n.Contains("Liberation") || n.Contains("Arial");
        }

        public static void ApplyAllInCanvas(Canvas canvas)
        {
            if (canvas == null) return;
            WarmUpAndSetDefault();
            foreach (var tmp in canvas.GetComponentsInChildren<TextMeshProUGUI>(true))
                Apply(tmp);
        }

        public static void ApplyAllLoaded()
        {
            WarmUpAndSetDefault();
            foreach (var tmp in Object.FindObjectsByType<TextMeshProUGUI>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
                Apply(tmp);
        }

        private static void SetAsTmpDefault(TMP_FontAsset font)
        {
            if (font == null || TMP_Settings.instance == null) return;
            if (NeedsJapaneseFont(TMP_Settings.defaultFontAsset) || !IsUsableJapaneseFont(TMP_Settings.defaultFontAsset))
                TMP_Settings.defaultFontAsset = font;
        }

        private static bool TryEnsureBaked(TMP_FontAsset font)
        {
            if (font == null) return false;

            if (HasBakedGlyphs(font))
            {
                BindAtlasToMaterial(font);
                return true;
            }

            if (font.atlasPopulationMode == AtlasPopulationMode.Dynamic)
            {
                GetUpdateFontAssetDataMethod()?.Invoke(font, null);
                font.TryAddCharacters(PreWarmCharacters);
                font.ReadFontAssetDefinition();
            }

            BindAtlasToMaterial(font);
            return HasBakedGlyphs(font);
        }

        private static MethodInfo GetUpdateFontAssetDataMethod()
        {
            if (!_updateFontAssetDataMethodResolved)
            {
                _updateFontAssetDataMethod = typeof(TMP_FontAsset).GetMethod(
                    "UpdateFontAssetData",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                _updateFontAssetDataMethodResolved = true;
            }
            return _updateFontAssetDataMethod;
        }

        private static bool IsUsableJapaneseFont(TMP_FontAsset font)
        {
            if (font == null) return false;
            if (IsNonJapaneseFallbackFont(font)) return false;
            return HasBakedGlyphs(font);
        }

        private static TMP_FontAsset EnsureCharactersForText(TMP_FontAsset font, string text)
        {
            if (font == null || string.IsNullOrEmpty(text)) return font;

            if (font.atlasPopulationMode == AtlasPopulationMode.Dynamic)
            {
                font.TryAddCharacters(text);
                font.ReadFontAssetDefinition();
                BindAtlasToMaterial(font);
            }

            if (ContainsAllCharacters(font, text))
                return font;

            var dynamicFont = CreateDynamicFromTtf();
            if (dynamicFont == null) return font;

            dynamicFont.TryAddCharacters(PreWarmCharacters + text);
            dynamicFont.ReadFontAssetDefinition();
            BindAtlasToMaterial(dynamicFont);

            if (ContainsAllCharacters(dynamicFont, text))
            {
                _cached = dynamicFont;
                SetAsTmpDefault(dynamicFont);
                return dynamicFont;
            }

            return font;
        }

        private static bool ContainsAllCharacters(TMP_FontAsset font, string text)
        {
            if (font == null || string.IsNullOrEmpty(text)) return true;
            font.ReadFontAssetDefinition();

            foreach (char c in text)
            {
                if (char.IsControl(c) || char.IsWhiteSpace(c)) continue;
                if (!font.characterLookupTable.ContainsKey((uint)c))
                    return false;
            }

            return true;
        }

        private static bool HasBakedGlyphs(TMP_FontAsset font)
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

        /// <summary>
        /// 共有アセットを書き換えないよう、Dynamic フォントをランタイム専用インスタンスへ複製する。
        /// マテリアル・アトラステクスチャも複製し、元アセットとの共有を断つ。
        /// </summary>
        private static TMP_FontAsset CreateRuntimeInstance(TMP_FontAsset source)
        {
            if (source == null) return null;

            var instance = Object.Instantiate(source);
            instance.name = source.name + " (Runtime Copy)";

            if (source.material != null)
                instance.material = Object.Instantiate(source.material);

            if (source.atlasTextures != null && source.atlasTextures.Length > 0)
            {
                // マルチアトラス（複数ページ）構成の場合、全ページを同じ順序で複製しないと
                // TMP_MaterialManager が atlasIndex でアクセスした際に範囲外エラーになる。
                var atlasCopies = new Texture2D[source.atlasTextures.Length];
                for (int i = 0; i < source.atlasTextures.Length; i++)
                {
                    atlasCopies[i] = source.atlasTextures[i] != null
                        ? Object.Instantiate(source.atlasTextures[i])
                        : null;
                }
                instance.atlasTextures = atlasCopies;
            }

            return instance;
        }

        private static TMP_FontAsset CreateDynamicFromTtf()
        {
            var ttf = Resources.Load<Font>(TtfResourcePath);
            if (ttf == null) return null;

            var asset = CreateFontAssetViaReflection(ttf) ?? TMP_FontAsset.CreateFontAsset(ttf);
            if (asset == null) return null;

            asset.name = RuntimeFontName;
            asset.atlasPopulationMode = AtlasPopulationMode.Dynamic;
            BindAtlasToMaterial(asset);
            return asset;
        }

        private static TMP_FontAsset CreateFontAssetViaReflection(Font ttf)
        {
            foreach (var m in typeof(TMP_FontAsset).GetMethods(BindingFlags.Public | BindingFlags.Static))
            {
                if (m.Name != "CreateFontAsset") continue;
                var p = m.GetParameters();
                if (p.Length < 6 || p[0].ParameterType != typeof(Font)) continue;

                try
                {
                    object[] args = p.Length switch
                    {
                        >= 8 => new object[]
                        {
                            ttf, SamplingPointSize, AtlasPadding, GlyphRenderModeSdfAa,
                            AtlasSize, AtlasSize, AtlasPopulationDynamic, true
                        },
                        7 => new object[]
                        {
                            ttf, SamplingPointSize, AtlasPadding, GlyphRenderModeSdfAa,
                            AtlasSize, AtlasSize, AtlasPopulationDynamic
                        },
                        6 => new object[]
                        {
                            ttf, SamplingPointSize, AtlasPadding, GlyphRenderModeSdfAa,
                            AtlasSize, AtlasSize
                        },
                        _ => null
                    };
                    if (args == null) continue;
                    return m.Invoke(null, args) as TMP_FontAsset;
                }
                catch
                {
                    // 別オーバーロードを試す
                }
            }
            return null;
        }

        private static void BindAtlasToMaterial(TMP_FontAsset font)
        {
            if (font?.material == null) return;
            var atlas = font.atlasTexture;
            if (atlas == null) return;

            font.material.SetTexture(ShaderUtilities.ID_MainTex, atlas);
            font.material.SetFloat(ShaderUtilities.ID_TextureWidth, font.atlasWidth);
            font.material.SetFloat(ShaderUtilities.ID_TextureHeight, font.atlasHeight);
        }
    }
}
