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

        public int ChoiceCount => Choices?.Length ?? 0;

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
