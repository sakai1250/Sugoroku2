using System.Collections;
using System.Linq;
using UnityEngine;
using Sugoroku.Data;

namespace Sugoroku.Game
{
    /// <summary>requirements の「中間発表」演出: 定期的に全員のスコアを比較し、首位ボーナス/最下位デバフを与える。</summary>
    public static class MidGameAnnouncementRules
    {
        public const int TopIfBonus     = 5;
        public const int TopVirtueBonus = 2;
        public const int BottomMentalPenalty = -10;

        public static IEnumerator Resolve(PlayerData[] players)
        {
            var active = players?.Where(p => p != null && p.Status == PlayerStatus.Active).ToList();
            if (active == null || active.Count < 2) yield break;

            var ranked = active.OrderByDescending(ScoreCalculator.Total).ToList();
            var top = ranked.First();
            var bottom = ranked.Last();

            yield return Sugoroku.UI.EventIntroPresenter.PlayAnnouncement(
                "★ 中間発表 ★",
                $"現在の首位は {PlayerIdentity.FormatHudLabel(top)}（{ScoreCalculator.Total(top)} pt）",
                Sugoroku.UI.CutInStyle.Split, strong: false);

            Sugoroku.UI.GameStatusBanner.Show(
                $"★ 中間発表！ 現在の首位は {PlayerIdentity.FormatHudLabel(top)}（{ScoreCalculator.Total(top)} pt）");
            yield return new WaitForSeconds(GameConfig.AnimationDuration(0.6f));

            yield return StatChangeSequencer.Apply(top, 0, TopIfBonus, 0, TopVirtueBonus);

            if (bottom != top)
            {
                Sugoroku.UI.GameStatusBanner.Show(
                    $"…そして {PlayerIdentity.FormatHudLabel(bottom)} には焦りの色が。メンタル {BottomMentalPenalty}");
                yield return new WaitForSeconds(GameConfig.AnimationDuration(0.6f));
                yield return StatChangeSequencer.Apply(bottom, 0, 0, BottomMentalPenalty, 0);
            }
        }
    }
}
