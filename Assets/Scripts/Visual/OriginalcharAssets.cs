using UnityEngine;
using Sugoroku.Data;

namespace Sugoroku.Visual
{
    /// <summary>
    /// キャラクター画像アセット提供クラス。
    /// 表情差分 (涙目:1, 真顔:2, 笑顔:3) に対応し、デフォルトは真顔(2)を返す。
    /// </summary>
    public static class OriginalcharAssets
    {
        public const string LegacyResourcesRoot = "ThirdParty/Kenney/Originalchar";

        private static readonly System.Collections.Generic.Dictionary<CharacterType, Sprite> _legacyCache = new();

        public static string GetFileName(CharacterType type) => type switch
        {
            CharacterType.Hobbyist => "多趣味",
            CharacterType.Serious  => "真面目",
            CharacterType.Athletic => "体育会系",
            CharacterType.Rich     => "金持ち",
            CharacterType.Genius   => "天才",
            _                      => "多趣味"
        };

        /// <summary>通常時（真顔: 2）のスプライトを返す。</summary>
        public static Sprite GetSprite(CharacterType type) =>
            CharacterVisualAssets.GetSprite(type, CharacterMood.Neutral);

        /// <summary>指定表情のスプライトを返す。</summary>
        public static Sprite GetSprite(CharacterType type, CharacterMood mood) =>
            CharacterVisualAssets.GetSprite(type, mood);

        /// <summary>従来の Kenney/Originalchar 画像（フォールバック用）</summary>
        public static Sprite GetLegacySprite(CharacterType type)
        {
            if (_legacyCache.TryGetValue(type, out var cached) && cached != null)
                return cached;

            string path = $"{LegacyResourcesRoot}/{GetFileName(type)}";
            var sprite = KenneyAssets.LoadSprite(path);
            if (sprite == null)
                sprite = LoadSpriteFromTexture(path);

            if (sprite != null)
                _legacyCache[type] = sprite;

            return sprite;
        }

        private static Sprite LoadSpriteFromTexture(string resourcesPath)
        {
            var tex = Resources.Load<Texture2D>(resourcesPath);
            if (tex == null) return null;

            return Sprite.Create(
                tex,
                new Rect(0, 0, tex.width, tex.height),
                new Vector2(0.5f, 0.5f),
                Mathf.Max(tex.width, tex.height) / 2f);
        }

        public static void ClearCache()
        {
            _legacyCache.Clear();
            CharacterVisualAssets.ClearCache();
        }
    }
}
