using System;

namespace Sugoroku.Data
{
    /// <summary>「査読中…」システム: 遅延選択の結果が届くまでプレイヤーごとに保持する状態。</summary>
    [Serializable]
    public class PendingEventResult
    {
        public string Label;
        public int    TurnsRemaining;
        public int    AcceptProbabilityPercent;
        public int    AcceptMoneyChange;
        public int    AcceptIfScoreChange;
        public int    AcceptMentalChange;
        public int    AcceptVirtueChange;
        public string AcceptedText;
        public int    RejectMoneyChange;
        public int    RejectIfScoreChange;
        public int    RejectMentalChange;
        public int    RejectVirtueChange;
        public string RejectedText;

        public static PendingEventResult FromChoice(EventChoice choice)
        {
            return new PendingEventResult
            {
                Label = string.IsNullOrEmpty(choice.PendingLabel) ? "結果待ち…" : choice.PendingLabel,
                TurnsRemaining = choice.DelayTurns,
                AcceptProbabilityPercent = choice.AcceptProbabilityPercent <= 0 ? 100 : choice.AcceptProbabilityPercent,
                AcceptMoneyChange = choice.AcceptMoneyChange,
                AcceptIfScoreChange = choice.AcceptIfScoreChange,
                AcceptMentalChange = choice.AcceptMentalChange,
                AcceptVirtueChange = choice.AcceptVirtueChange,
                AcceptedText = choice.AcceptedText,
                RejectMoneyChange = choice.RejectMoneyChange,
                RejectIfScoreChange = choice.RejectIfScoreChange,
                RejectMentalChange = choice.RejectMentalChange,
                RejectVirtueChange = choice.RejectVirtueChange,
                RejectedText = choice.RejectedText,
            };
        }
    }
}
