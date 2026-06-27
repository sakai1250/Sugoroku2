using System.Collections;
using Sugoroku.Data;
using UnityEngine;

namespace Sugoroku.Game
{
    /// <summary>所持金・IF・メンタル・徳を1項目ずつ適用し、変化が分かるよう演出間隔を空ける。</summary>
    public static class StatChangeSequencer
    {
        /// <summary>フロート表示が終わってから次のステータスへ進む待ち時間。</summary>
        public static float StepDelaySeconds =>
            GameConfig.FloatingTextDuration + GameConfig.StatFloatGapSeconds;

        public static IEnumerator Apply(PlayerData player, int money, int ifScore, int mental, int virtue)
        {
            if (player == null) yield break;

            float delay = GameConfig.AnimationDuration(StepDelaySeconds);

            if (money != 0)
            {
                player.ApplyStatChange(money, 0, 0, 0);
                yield return new WaitForSeconds(delay);
            }

            if (ifScore != 0)
            {
                player.ApplyStatChange(0, ifScore, 0, 0);
                yield return new WaitForSeconds(delay);
            }

            if (mental != 0)
            {
                player.ApplyStatChange(0, 0, mental, 0);
                yield return new WaitForSeconds(delay);
            }

            if (virtue != 0)
                player.ApplyStatChange(0, 0, 0, virtue);
        }
    }
}
