using System.Collections.Generic;
using Sugoroku.Data;
using UnityEngine;

namespace Sugoroku.Board
{
    /// <summary>Resources/event-MASU のマス用イラストを SquareType / イベントタグから解決する。</summary>
    public static class EventMasuArt
    {
        public enum Category
        {
            None,
            Event,
            Research,
            Lab,
            Economy
        }

        private const string ResourceFolder = "event-MASU";

        private static readonly Dictionary<string, Sprite> SpriteCache = new();

        public static Category ResolveCategory(SquareType type, string[] tags)
        {
            if (tags != null && tags.Length > 0)
            {
                foreach (var tag in tags)
                {
                    if (tag is "進路" or "分岐")
                        return Category.Lab;
                }

                foreach (var tag in tags)
                {
                    if (tag is "研究" or "学会")
                        return Category.Research;
                }

                foreach (var tag in tags)
                {
                    if (tag is "ゼミ" or "教授" or "後輩")
                        return Category.Lab;
                }

                foreach (var tag in tags)
                {
                    if (tag is "バイト" or "生活" or "事務")
                        return Category.Economy;
                }

                return Category.Event;
            }

            return type switch
            {
                SquareType.Event   => Category.Event,
                SquareType.Journal => Category.Research,
                SquareType.Lecture => Category.Lab,
                SquareType.Start   => Category.Lab,
                SquareType.Goal    => Category.Research,
                SquareType.Tuition => Category.Economy,
                SquareType.PartTime => Category.Economy,
                SquareType.Rest    => Category.Lab,
                SquareType.Bonus   => Category.Event,
                SquareType.Penalty => Category.Event,
                SquareType.Branch  => Category.Lab,
                _                  => Category.None
            };
        }

        public static Sprite GetCardSprite(Category category) => GetSprite(category, pixelStyle: true);

        public static Sprite GetHeroSprite(Category category) => GetSprite(category, pixelStyle: false);

        public static Sprite GetHeroSprite(SquareType type, string[] tags) =>
            GetHeroSprite(ResolveCategory(type, tags));

        public static Sprite GetSprite(Category category, bool pixelStyle)
        {
            if (category == Category.None)
                return null;

            string baseName = category switch
            {
                Category.Event    => "イベント",
                Category.Research => "研究",
                Category.Lab      => "ラボ",
                Category.Economy  => "経済",
                _                 => null
            };

            if (string.IsNullOrEmpty(baseName))
                return null;

            string fileName = pixelStyle ? $"{baseName}ドット" : baseName;

            if (SpriteCache.TryGetValue(fileName, out var cached) && cached != null)
                return cached;

            string path = $"{ResourceFolder}/{fileName}";
            var sprite = LoadSpriteOrTexture(path);

            // pixelStyle のファイルが存在しない場合は通常版 (baseName) にフォールバック
            if (sprite == null && pixelStyle)
            {
                if (SpriteCache.TryGetValue(baseName, out var baseCached) && baseCached != null)
                    return baseCached;
                sprite = LoadSpriteOrTexture($"{ResourceFolder}/{baseName}");
            }

            if (sprite != null)
                SpriteCache[fileName] = sprite;

            return sprite;
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
                    100f);
            }
            return null;
        }

        public static void ClearCache() => SpriteCache.Clear();

        public static void Prewarm(bool pixelStyle = true)
        {
            GetSprite(Category.Event, pixelStyle);
            GetSprite(Category.Research, pixelStyle);
            GetSprite(Category.Lab, pixelStyle);
            GetSprite(Category.Economy, pixelStyle);
        }
    }
}
