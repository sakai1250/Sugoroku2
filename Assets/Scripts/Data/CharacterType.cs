using UnityEngine;

namespace Sugoroku.Data
{
    public enum CharacterType
    {
        Hobbyist,
        Serious,
        Athletic,
        Rich,
        Genius
    }

    public readonly struct CharacterProfile
    {
        public readonly int Money;
        public readonly int Mental;
        public readonly int MaxMental;
        public readonly int IfScore;
        public readonly int Virtue;

        public CharacterProfile(int money, int mental, int maxMental, int ifScore, int virtue)
        {
            Money = money; Mental = mental; MaxMental = maxMental; IfScore = ifScore; Virtue = virtue;
        }
    }

    public static class CharacterTypeExtensions
    {
        public static string DisplayName(this CharacterType type) => type switch
        {
            CharacterType.Hobbyist => "多趣味系",
            CharacterType.Serious  => "真面目系",
            CharacterType.Athletic => "体育会系",
            CharacterType.Rich     => "金持ち系",
            CharacterType.Genius   => "天才肌",
            _                      => "不明"
        };

        public static string EnglishName(this CharacterType type) => type switch
        {
            CharacterType.Hobbyist => "Hobbyist",
            CharacterType.Serious  => "Serious",
            CharacterType.Athletic => "Athletic",
            CharacterType.Rich     => "Rich",
            CharacterType.Genius   => "Genius",
            _                      => ""
        };

        public static string TraitName(this CharacterType type) => type switch
        {
            CharacterType.Hobbyist => "一旦逃避",
            CharacterType.Serious  => "講義マスター",
            CharacterType.Athletic => "鋼の肉体",
            CharacterType.Rich     => "経済的解決",
            CharacterType.Genius   => "乱数調整",
            _                      => "なし"
        };

        public static string SkillName(this CharacterType type) => TraitName(type);

        public static string TraitDescription(this CharacterType type) => type switch
        {
            CharacterType.Hobbyist => "1回休みと引き換えにメンタル全快＋次のイベント回避。",
            CharacterType.Serious  => "講義マスのデバフ無効。IF獲得効率+10%。",
            CharacterType.Athletic => "メンタル減少値を常に50%カット（端数切捨て）。",
            CharacterType.Rich     => "マイナスイベントを所持金支払いで無効化。",
            CharacterType.Genius   => "ジャーナルマス等で IF 獲得時に +1〜6 pt のボーナス（1回の加算は上限あり）。",
            _                      => ""
        };

        public static string SkillDescription(this CharacterType type) => TraitDescription(type);

        public static string StrategicRole(this CharacterType type) => type switch
        {
            CharacterType.Hobbyist => "事故死が少なく、安定した長期戦に強い。",
            CharacterType.Serious  => "堅実にIFを稼ぐが、突発イベントでのダメージが大きい。",
            CharacterType.Athletic => "高負荷な研究イベントを強引に突破できる。",
            CharacterType.Rich     => "金をIFとメンタルに変換する「金で解決」プレイ。",
            CharacterType.Genius   => "爆発力No.1だが、メンタルが低く常に失踪と隣り合わせ。",
            _                      => ""
        };

        public const int RichBonusMoney = 10;

        public static CharacterProfile GetProfile(this CharacterType type) => type switch
        {
            CharacterType.Hobbyist => new(GameConfig.InitialMoney, GameConfig.InitialMental, GameConfig.MaxMental, 0, 0),
            CharacterType.Serious  => new(GameConfig.InitialMoney, GameConfig.InitialMental, GameConfig.MaxMental, 0, 0),
            CharacterType.Athletic => new(GameConfig.InitialMoney, GameConfig.InitialMental, GameConfig.MaxMental, 0, 0),
            CharacterType.Rich     => new(GameConfig.InitialMoney + RichBonusMoney, GameConfig.InitialMental, GameConfig.MaxMental, 0, 0),
            CharacterType.Genius   => new(GameConfig.InitialMoney, GeniusInitialMental, GeniusInitialMental, 0, 0),
            _                      => new(GameConfig.InitialMoney, GameConfig.InitialMental, GameConfig.MaxMental, 0, 0)
        };

        /// <summary>天才肌は標準の 60% メンタル（旧 30/50 と同比率）。</summary>
        public static int GeniusInitialMental =>
            Mathf.RoundToInt(GameConfig.InitialMental * 0.6f);

        public static Color AccentColor(this CharacterType type) => type switch
        {
            CharacterType.Hobbyist => new Color(0.4f, 0.85f, 0.55f),
            CharacterType.Serious  => new Color(0.45f, 0.65f, 1f),
            CharacterType.Athletic => new Color(1f, 0.55f, 0.35f),
            CharacterType.Rich     => new Color(1f, 0.85f, 0.25f),
            CharacterType.Genius   => new Color(0.75f, 0.45f, 1f),
            _                      => Color.white
        };
    }
}
