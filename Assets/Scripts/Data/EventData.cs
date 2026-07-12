using System;
using System.Collections.Generic;

namespace Sugoroku.Data
{
    [Serializable]
    public class EventChoice
    {
        public string Label;
        /// <summary>例: "Money >= 50", "Virtue >= 15"。単一選択肢イベントには設定禁止。</summary>
        public string Conditions;
        public int    MoneyChange;
        public int    IfScoreChange;
        public int    MentalChange;
        public int    VirtueChange;

        /// <summary>「査読中…」システム: 0=即時（従来通り）。1以上でこのターン数後に結果が届く。</summary>
        public int    DelayTurns;
        /// <summary>結果待ちの間バナー等に表示するラベル（例: 「査読中…」）。</summary>
        public string PendingLabel;
        /// <summary>採択される確率(0-100)。DelayTurns=0の場合は無視。</summary>
        public int    AcceptProbabilityPercent = 100;
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

        /// <summary>分岐ルート選択イベント用。"Lab" / "PartTime" のいずれかを設定するとプレイヤーの進行ルートを切り替える。</summary>
        public string SetsBranchRoute;
    }

    /// <summary>
    /// JsonUtility 互換のため配列を使用（List はデシリアライズされないことがある）。
    /// </summary>
    [Serializable]
    public class EventMaster
    {
        public string         EventId;
        public string[]       Tags;
        public string         Title;
        public string         Description;
        public EventChoice[]  Choices;
        /// <summary>"Rare" のときのみ超レアイベント抽選プールの対象になる。未指定は通常イベント扱い。</summary>
        public string         Rarity;

        public int ChoiceCount => Choices?.Length ?? 0;

        public bool IsRare => Rarity == "Rare";

        public EventChoice GetChoice(int index) =>
            Choices != null && index >= 0 && index < Choices.Length ? Choices[index] : null;
    }

    [Serializable]
    public class EventMasterList
    {
        public EventMaster[] Events;

        public List<EventMaster> ToList()
        {
            if (Events == null || Events.Length == 0)
                return new List<EventMaster>();
            return new List<EventMaster>(Events);
        }
    }
}
