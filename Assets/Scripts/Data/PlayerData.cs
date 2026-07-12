using System.Collections.Generic;
using UnityEngine;

namespace Sugoroku.Data
{
    public enum PlayerStatus { Active, Graduated, Dropout }

    /// <summary>盤面分岐ルート: 研究室(IF重視・メンタル消耗) / バイト(金重視・IF停滞)。</summary>
    public enum BranchRoute { None, Lab, PartTime }

    [System.Serializable]
    public class PlayerData
    {
        public int           Index;
        public string        Name;
        public CharacterType Character;
        public bool          IsCpu;

        public int Money;
        public int IfScore;
        public int Mental;
        public int MaxMental;
        public int Virtue;

        public int  BoardPosition;
        public int  SkipTurns;
        public int  IgnoreNextEvents;
        public bool SkillUsedThisTurn;
        public bool GeniusBonusActive;
        public bool AthleticMentalImmunity;

        public PlayerStatus Status;
        public int          FinishRank;
        public Color        PieceTint = Color.white;

        /// <summary>「査読中…」等、遅延イベントの結果待ちキュー。</summary>
        public List<PendingEventResult> PendingResults = new();

        /// <summary>分岐ルート選択の状態。合流マス通過時に None へリセットされる。</summary>
        public BranchRoute ActiveBranch = BranchRoute.None;

        /// <summary>ゾロ目演出: 直前の出目。次のダイスがこれと一致すると「ゾロ目」判定になる。</summary>
        public int  LastDiceValue = -1;
        /// <summary>ゾロ目でもう1回振れる権利を得ている状態。</summary>
        public bool HasExtraRoll;

        /// <summary>実績用: 一度でも所持金が崖っぷちラインまで落ちたか。</summary>
        public bool SurvivedBankruptcyScare;

        /// <summary>リザルト画面の推移グラフ用。EndTurn()の度に1件追加される。</summary>
        public List<StatSnapshot> History = new();
        /// <summary>このプレイヤーが経過した個人ターン数。Historyの記録に使う。</summary>
        public int TurnsTaken;

        /// <summary>アイテム所持数。バイトマス通過時に確率で拾得する。</summary>
        public int ItemDiceRerollCount;
        public int ItemMentalHealCount;
        public int ItemMoneyBonusCount;

        public static PlayerData Create(int index, CharacterType character, bool isCpu)
        {
            var profile = character.GetProfile();
            return new PlayerData
            {
                Index            = index,
                Name             = isCpu ? $"CPU" : $"P{index + 1}",
                Character        = character,
                IsCpu            = isCpu,
                Money            = profile.Money,
                IfScore          = profile.IfScore,
                Mental           = profile.Mental,
                MaxMental        = profile.MaxMental,
                Virtue           = profile.Virtue,
                BoardPosition    = 0,
                SkipTurns        = 0,
                IgnoreNextEvents = 0,
                SkillUsedThisTurn= false,
                GeniusBonusActive= false,
                Status           = PlayerStatus.Active,
                FinishRank       = 0
            };
        }

        public bool IsEliminated => Status == PlayerStatus.Dropout;
        public bool IsFinished   => Status != PlayerStatus.Active;

        public void ApplyStatChange(int money, int ifScore, int mental, int virtue)
        {
            ApplyStatChange(money, ifScore, mental, virtue, applyCharacterPassives: true);
        }

        public void ApplyStatChange(int money, int ifScore, int mental, int virtue, bool applyCharacterPassives)
        {
            if (applyCharacterPassives)
            {
                if (Character == CharacterType.Athletic && mental < 0)
                {
                    if (AthleticMentalImmunity || SkillUsedThisTurn)
                        mental = 0;
                    else
                        mental = mental / 2;
                }

                if (Character == CharacterType.Hobbyist && mental <= -15)
                {
                    SkipTurns++;
                    mental = 0;
                }

                if (Character == CharacterType.Rich && money < 0 && Money >= GameConfig.RichSkillCost)
                    money = -GameConfig.RichSkillCost;
            }

            int prevMoney  = Money;
            int prevIf     = IfScore;
            int prevMental = Mental;
            int prevVirtue = Virtue;

            Money   = PlayerStatRules.ClampMoney(Money + money);
            IfScore = PlayerStatRules.ClampIfScore(IfScore + ifScore);
            Mental  = PlayerStatRules.ClampMental(Mental + mental, MaxMental);
            Virtue += virtue;

            if (Money <= AchievementEvaluator.BankruptcyScareThreshold)
                SurvivedBankruptcyScare = true;

            StatChangeNotifier.Notify(this,
                Money - prevMoney,
                IfScore - prevIf,
                Mental - prevMental,
                Virtue - prevVirtue);
        }

        public int CalculateScore() => ScoreCalculator.Total(this);
    }
}
