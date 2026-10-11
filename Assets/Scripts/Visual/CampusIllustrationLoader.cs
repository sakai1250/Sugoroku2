using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using Sugoroku.Data;

namespace Sugoroku.Visual
{
    /// <summary>
    /// 3大新ステージイラスト（研究室.png, 学会.png, 居酒屋.png）のロードとキャッシュを管理する。
    /// 各シーズンに応じた美麗な背景スプライトを確実に供給する。
    /// </summary>
    public static class CampusIllustrationLoader
    {
        private static readonly Dictionary<SeasonType, Sprite> _cache = new();
        private static bool _initialized = false;

        public static void WarmUp()
        {
            if (_initialized) return;

            LoadIllustration(SeasonType.ResearchWriting, "研究室");
            LoadIllustration(SeasonType.Conference, "学会");
            LoadIllustration(SeasonType.PartTime, "居酒屋");

            _initialized = true;
        }

        public static Sprite GetIllustration(SeasonType season)
        {
            if (!_initialized) WarmUp();

            if (_cache.TryGetValue(season, out var sprite) && sprite != null)
                return sprite;

            string name = season switch
            {
                SeasonType.ResearchWriting => "研究室",
                SeasonType.Conference      => "学会",
                SeasonType.PartTime        => "居酒屋",
                _                          => "研究室"
            };
            return LoadIllustration(season, name);
        }

        private static Sprite LoadIllustration(SeasonType season, string baseName)
        {
            // 1. Resources.Load からの直接取得
            var sprite = Resources.Load<Sprite>($"goods/{baseName}");
            if (sprite != null)
            {
                _cache[season] = sprite;
                return sprite;
            }

            // 2. Resources.Load<Texture2D> からの Sprite 作成
            var tex = Resources.Load<Texture2D>($"goods/{baseName}");
            if (tex != null)
            {
                sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
                sprite.name = baseName;
                _cache[season] = sprite;
                return sprite;
            }

            // 3. ファイルパス直接ロード（NFC / NFD 両対応フォールバック）
            string folder = Path.Combine(Application.dataPath, "Resources", "goods");
            string pathNfc = Path.Combine(folder, baseName.Normalize(System.Text.NormalizationForm.FormC) + ".png");
            string pathNfd = Path.Combine(folder, baseName.Normalize(System.Text.NormalizationForm.FormD) + ".png");

            string targetPath = File.Exists(pathNfc) ? pathNfc : (File.Exists(pathNfd) ? pathNfd : null);
            if (targetPath != null)
            {
                try
                {
                    byte[] bytes = File.ReadAllBytes(targetPath);
                    var directTex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                    if (directTex.LoadImage(bytes))
                    {
                        directTex.filterMode = FilterMode.Bilinear;
                        sprite = Sprite.Create(directTex, new Rect(0, 0, directTex.width, directTex.height), new Vector2(0.5f, 0.5f), 100f);
                        sprite.name = baseName;
                        _cache[season] = sprite;
                        Debug.Log($"[CampusIllustrationLoader] Loaded {baseName}.png directly from disk ({directTex.width}x{directTex.height}).");
                        return sprite;
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[CampusIllustrationLoader] Direct read failed for {baseName}: {ex.Message}");
                }
            }

            Debug.LogWarning($"[CampusIllustrationLoader] Could not load illustration for season {season} ({baseName}.png).");
            return null;
        }

        public static void Clear()
        {
            _cache.Clear();
            _initialized = false;
        }
    }
}
