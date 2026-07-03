using System.Linq;

namespace Sugoroku.Data
{
    /// <summary>イベントのタグとキャラ特性の相性で、ステータス変動を補正する。</summary>
    public static class CharacterTagAffinity
    {
        private static readonly string[] AthleticTags = { "トラブル", "緊急" };
        private static readonly string[] SeriousTags   = { "研究", "査読", "学会" };
        private static readonly string[] RichTags      = { "生活", "バイト", "事務", "研究費" };
        private static readonly string[] GeniusTags    = { "進路", "分岐" };
        private static readonly string[] HobbyistTags  = { "生活", "バイト" };

        public static string[] GetAffinityTags(CharacterType character) => character switch
        {
            CharacterType.Athletic => AthleticTags,
            CharacterType.Serious  => SeriousTags,
            CharacterType.Rich     => RichTags,
            CharacterType.Genius   => GeniusTags,
            CharacterType.Hobbyist => HobbyistTags,
            _                      => System.Array.Empty<string>()
        };

        public static bool HasAffinity(CharacterType character, string[] tags)
        {
            if (tags == null || tags.Length == 0) return false;
            var affinity = GetAffinityTags(character);
            return affinity.Length > 0 && tags.Any(t => affinity.Contains(t));
        }

        /// <summary>タグ相性がある場合にステータス変動を補正する。</summary>
        public static (int money, int ifScore, int mental, int virtue) Apply(
            CharacterType character, string[] tags, int money, int ifScore, int mental, int virtue)
        {
            if (!HasAffinity(character, tags))
                return (money, ifScore, mental, virtue);

            switch (character)
            {
                case CharacterType.Athletic:
                    if (mental < 0) mental = System.Math.Min(0, mental + 5);
                    break;
                case CharacterType.Serious:
                    if (ifScore > 0) ifScore += System.Math.Max(1, ifScore / 5);
                    break;
                case CharacterType.Rich:
                    if (money > 0) money += System.Math.Max(1, money / 5);
                    else if (money < 0) money -= System.Math.Max(1, -money / 10);
                    break;
                case CharacterType.Genius:
                    if (money > 0) money += System.Math.Max(1, money / 5);
                    if (ifScore > 0) ifScore += System.Math.Max(1, ifScore / 5);
                    break;
                case CharacterType.Hobbyist:
                    if (mental < 0) mental = System.Math.Min(0, mental + 3);
                    else if (mental > 0) mental += 1;
                    break;
            }

            return (money, ifScore, mental, virtue);
        }
    }
}
