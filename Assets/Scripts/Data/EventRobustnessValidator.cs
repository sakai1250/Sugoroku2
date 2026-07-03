using System.Collections.Generic;
using System.Text;

namespace Sugoroku.Data
{
    /// <summary>requirements §7.1 — イベント進行のソフトロック防止。</summary>
    public static class EventRobustnessValidator
    {
        public struct Issue
        {
            public string EventId;
            public string Message;
        }

        public static bool HasConditions(EventChoice choice) =>
            choice != null && !string.IsNullOrWhiteSpace(choice.Conditions);

        /// <summary>単一選択肢は conditions を無視して常に選べる。</summary>
        public static bool CanSelectChoice(EventMaster ev, EventChoice choice, PlayerData player)
        {
            if (choice == null || player == null) return false;
            if (ev != null && ev.ChoiceCount == 1) return true;
            return EventChoiceEvaluator.MeetsConditions(choice, player, out _);
        }

        public static int CountSelectableChoices(EventMaster ev, PlayerData player)
        {
            if (ev == null || player == null) return 0;
            int count = 0;
            for (int i = 0; i < ev.ChoiceCount; i++)
            {
                if (CanSelectChoice(ev, ev.GetChoice(i), player))
                    count++;
            }
            return count;
        }

        /// <summary>複数選択肢で、条件なしの代替肢が1つ以上あるか。</summary>
        public static bool HasUnconditionalFallback(EventMaster ev)
        {
            if (ev == null || ev.ChoiceCount <= 1) return true;
            for (int i = 0; i < ev.ChoiceCount; i++)
            {
                if (!HasConditions(ev.GetChoice(i)))
                    return true;
            }
            return false;
        }

        public static int FirstUnconditionalIndex(EventMaster ev)
        {
            if (ev == null) return -1;
            for (int i = 0; i < ev.ChoiceCount; i++)
            {
                if (!HasConditions(ev.GetChoice(i)))
                    return i;
            }
            return -1;
        }

        public static int FirstSelectableIndex(EventMaster ev, PlayerData player)
        {
            if (ev == null || player == null) return -1;
            for (int i = 0; i < ev.ChoiceCount; i++)
            {
                if (CanSelectChoice(ev, ev.GetChoice(i), player))
                    return i;
            }
            return -1;
        }

        public static bool CanUseVirtueRescue(PlayerData player) =>
            player != null && !player.IsCpu && player.Virtue >= GameConfig.VirtueRescueThreshold;

        public static List<Issue> ValidateAll(IEnumerable<EventMaster> events)
        {
            var issues = new List<Issue>();
            if (events == null) return issues;

            foreach (var ev in events)
            {
                if (ev == null) continue;
                if (ev.ChoiceCount == 0)
                {
                    issues.Add(new Issue { EventId = ev.EventId, Message = "選択肢が0件です。" });
                    continue;
                }

                if (ev.ChoiceCount == 1 && HasConditions(ev.GetChoice(0)))
                {
                    issues.Add(new Issue
                    {
                        EventId = ev.EventId,
                        Message = "単一選択肢に conditions が設定されています（§7.1: 設定禁止）。"
                    });
                }

                if (ev.ChoiceCount > 1 && !HasUnconditionalFallback(ev))
                {
                    issues.Add(new Issue
                    {
                        EventId = ev.EventId,
                        Message = "複数選択肢ですが、条件なしの代替肢がありません（§7.1）。"
                    });
                }

                for (int i = 0; i < ev.ChoiceCount; i++)
                {
                    var choice = ev.GetChoice(i);
                    if (choice == null || choice.DelayTurns <= 0) continue;

                    if (string.IsNullOrEmpty(choice.AcceptedText) || string.IsNullOrEmpty(choice.RejectedText))
                        issues.Add(new Issue
                        {
                            EventId = ev.EventId,
                            Message = $"選択肢{i}: DelayTurns>0 だが AcceptedText/RejectedText が未設定です（査読中…システム）。"
                        });

                    if (choice.AcceptProbabilityPercent < 0 || choice.AcceptProbabilityPercent > 100)
                        issues.Add(new Issue
                        {
                            EventId = ev.EventId,
                            Message = $"選択肢{i}: AcceptProbabilityPercent が 0-100 の範囲外です。"
                        });
                }
            }
            return issues;
        }

        public static string FormatReport(IList<Issue> issues)
        {
            if (issues == null || issues.Count == 0)
                return "§7.1 検証: 問題なし";

            var sb = new StringBuilder();
            sb.AppendLine($"§7.1 検証: {issues.Count} 件の問題");
            foreach (var i in issues)
                sb.AppendLine($"  [{i.EventId}] {i.Message}");
            return sb.ToString();
        }
    }
}
