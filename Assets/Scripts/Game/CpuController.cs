using UnityEngine;
using Sugoroku.Data;

namespace Sugoroku.Game
{
    public class CpuController : MonoBehaviour
    {
        public static CpuController Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        public void DecideAction(PlayerData player)
        {
            // CPU は常にダイスを振る（スキルは低確率で使用）
            if (!player.SkillUsedThisTurn && ShouldUseSkill(player))
            {
                GameManager.Instance.UsePlayerSkill(player);
                return;
            }
            DiceRoller.Instance.Roll();
        }

        public int PickChoice(PlayerData player, EventMaster ev)
        {
            int guaranteed = EventRobustnessValidator.FirstSelectableIndex(ev, player);
            if (guaranteed >= 0 && ev.ChoiceCount == 1)
                return guaranteed;

            // メンタルが低い場合は回復を優先
            if (player.Mental <= 30)
            {
                int bestIdx = -1;
                int bestMental = int.MinValue;
                for (int i = 0; i < ev.ChoiceCount; i++)
                {
                    var c = ev.GetChoice(i);
                    if (!EventRobustnessValidator.CanSelectChoice(ev, c, player)) continue;
                    if (c.MentalChange > bestMental)
                    {
                        bestMental = c.MentalChange;
                        bestIdx = i;
                    }
                }
                if (bestIdx >= 0) return bestIdx;
            }

            if (player.Money <= 10)
            {
                int bestIdx = -1;
                int bestMoney = int.MinValue;
                for (int i = 0; i < ev.ChoiceCount; i++)
                {
                    var c = ev.GetChoice(i);
                    if (!EventRobustnessValidator.CanSelectChoice(ev, c, player)) continue;
                    if (c.MoneyChange > bestMoney)
                    {
                        bestMoney = c.MoneyChange;
                        bestIdx = i;
                    }
                }
                if (bestIdx >= 0) return bestIdx;
            }

            int best = -1;
            float bestScore = float.MinValue;
            for (int i = 0; i < ev.ChoiceCount; i++)
            {
                var c = ev.GetChoice(i);
                if (!EventRobustnessValidator.CanSelectChoice(ev, c, player)) continue;
                float score = c.IfScoreChange * 3f + c.MentalChange + c.MoneyChange + c.VirtueChange;
                if (score > bestScore) { bestScore = score; best = i; }
            }
            if (best >= 0) return best;

            return EventRobustnessValidator.FirstSelectableIndex(ev, player);
        }

        private bool ShouldUseSkill(PlayerData player)
        {
            return player.Character == CharacterType.Athletic && player.Mental < 50
                || player.Character == CharacterType.Hobbyist && Random.value < 0.2f;
        }
    }
}
