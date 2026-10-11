using System.Collections.Generic;
using UnityEngine;
using Sugoroku.Data;

namespace Sugoroku.Visual
{
    /// <summary>
    /// キャラクターの表情（涙目:1, 真顔:2, 笑顔:3）
    /// </summary>
    public enum CharacterMood
    {
        Crying = 1,
        Neutral = 2,
        Smile = 3
    }

    /// <summary>
    /// Resources/character からキャラクター表情差分スプライトを読み込む。
    /// 表情仕様:
    /// - 1: 涙目（メンタル50%以下、またはメンタル下落時4秒）
    /// - 2: 真顔（通常時）
    /// - 3: 笑顔（パラメータ上昇時4秒）
    /// </summary>
    public static class CharacterVisualAssets
    {
        public const string ResourcesRoot = "character";

        private static readonly Dictionary<(CharacterType, CharacterMood), Sprite> Cache = new();

        public static string GetFilePrefix(CharacterType type) => type switch
        {
            CharacterType.Hobbyist => "Tasyumi",
            CharacterType.Serious  => "Majime",
            CharacterType.Athletic => "Taiiku",
            CharacterType.Rich     => "Kanemochi",
            CharacterType.Genius   => "Tensai",
            _                      => "Tasyumi"
        };

        public static Sprite GetSprite(CharacterType type, CharacterMood mood = CharacterMood.Neutral)
        {
            var key = (type, mood);
            if (Cache.TryGetValue(key, out var cached) && cached != null)
                return cached;

            string prefix = GetFilePrefix(type);
            int number = (int)mood;
            string fileName = $"{prefix}{number}";

            var sprite = LoadSpriteOrTexture($"{ResourcesRoot}/{fileName}");

            // Genius の 笑顔(3) が Tenasai3 になっている場合のフォールバック
            if (sprite == null && type == CharacterType.Genius && mood == CharacterMood.Smile)
            {
                sprite = LoadSpriteOrTexture($"{ResourcesRoot}/Tenasai3");
            }

            // フォールバック: 元の Kenney/Originalchar 画像
            if (sprite == null)
            {
                sprite = OriginalcharAssets.GetLegacySprite(type);
            }

            if (sprite != null)
                Cache[key] = sprite;

            return sprite;
        }

        public static Sprite GetSprite(CharacterType type, int moodNumber)
        {
            var mood = moodNumber switch
            {
                1 => CharacterMood.Crying,
                3 => CharacterMood.Smile,
                _ => CharacterMood.Neutral
            };
            return GetSprite(type, mood);
        }

        private static Sprite LoadSpriteOrTexture(string path)
        {
            var sprite = Resources.Load<Sprite>(path);
            if (sprite != null) return sprite;

            var tex = Resources.Load<Texture2D>(path);
            if (tex != null)
            {
                return Sprite.Create(
                    tex,
                    new Rect(0f, 0f, tex.width, tex.height),
                    new Vector2(0.5f, 0.5f),
                    Mathf.Max(tex.width, tex.height) / 2f);
            }

            return null;
        }

        public static void ClearCache() => Cache.Clear();
    }
}

