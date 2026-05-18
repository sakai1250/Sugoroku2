using UnityEngine;
using Sugoroku.Data;

namespace Sugoroku.Visual
{
    /// <summary>
    /// Originalchar フォルダのキャラ画像（PortraitImage・盤上の駒に使用）。
    /// Tools → Sugoroku → 一発セットアップ で Resources に同期する。
    /// </summary>
    public static class OriginalcharAssets
    {
        public const string ResourcesRoot = "ThirdParty/Kenney/Originalchar";

        private static readonly System.Collections.Generic.Dictionary<CharacterType, Sprite> _cache = new();

        public static string GetFileName(CharacterType type) => type switch
        {
            CharacterType.Hobbyist => "多趣味",
            CharacterType.Serious  => "真面目",
            CharacterType.Athletic => "体育会系",
            CharacterType.Rich     => "金持ち",
            CharacterType.Genius   => "天才",
            _                      => "多趣味"
        };

        public static Sprite GetSprite(CharacterType type)
        {
            if (_cache.TryGetValue(type, out var cached) && cached != null)
                return cached;

            string path = $"{ResourcesRoot}/{GetFileName(type)}";
            var sprite = KenneyAssets.LoadSprite(path);
            if (sprite == null)
                sprite = LoadSpriteFromTexture(path);

            if (sprite != null)
                _cache[type] = sprite;

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

        public static void ClearCache() => _cache.Clear();
    }
}
