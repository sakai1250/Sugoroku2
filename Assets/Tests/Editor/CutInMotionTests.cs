using System;
using NUnit.Framework;
using UnityEngine;
using Sugoroku.UI;
using Sugoroku.Data;

namespace Sugoroku.Tests.UI
{
    public class CutInMotionTests
    {
        private static readonly CutInStyle[] AllStyles = (CutInStyle[])Enum.GetValues(typeof(CutInStyle));

        [Test]
        public void Evaluate_AtEntranceEnd_ConvergesToRest([ValueSource(nameof(AllStyles))] CutInStyle style)
        {
            var (offset, scale, rot) = CutInMotion.Evaluate(style, 1f, 0f);

            Assert.That(offset.x, Is.EqualTo(0f).Within(0.001f), "offset.x");
            Assert.That(offset.y, Is.EqualTo(0f).Within(0.001f), "offset.y");
            Assert.That(scale.x, Is.EqualTo(1f).Within(0.001f), "scale.x");
            Assert.That(scale.y, Is.EqualTo(1f).Within(0.001f), "scale.y");
            Assert.That(rot, Is.EqualTo(0f).Within(0.001f), "rotZ");
        }

        [Test]
        public void Evaluate_AtStart_MatchesEntranceParams([ValueSource(nameof(AllStyles))] CutInStyle style)
        {
            var en = CutInMotion.Entrance(style);
            // t=0 のイージング値は 0 → 開始状態そのもの。
            var (offset, scale, rot) = CutInMotion.Evaluate(style, 0f, 0f);

            Assert.That(offset.x, Is.EqualTo(en.Offset.x).Within(0.001f));
            Assert.That(scale.x, Is.EqualTo(en.Scale.x).Within(0.001f));
            Assert.That(scale.y, Is.EqualTo(en.Scale.y).Within(0.001f));
            Assert.That(rot, Is.EqualTo(en.RotZ).Within(0.001f));
        }

        [Test]
        public void Evaluate_AtExitEnd_MatchesExitParams([ValueSource(nameof(AllStyles))] CutInStyle style)
        {
            var ex = CutInMotion.Exit(style);
            var (offset, scale, rot) = CutInMotion.Evaluate(style, 1f, 1f);

            Assert.That(offset.x, Is.EqualTo(ex.Offset.x).Within(0.001f), "offset.x");
            Assert.That(offset.y, Is.EqualTo(ex.Offset.y).Within(0.001f), "offset.y");
            Assert.That(scale.x, Is.EqualTo(ex.Scale.x).Within(0.001f), "scale.x");
            Assert.That(rot, Is.EqualTo(ex.RotZ).Within(0.001f), "rotZ");
        }

        [Test]
        public void PickEvent_TagsMapToExpectedStyles()
        {
            Assert.AreEqual(CutInStyle.BounceDown, CutInStyleMap.PickEvent(EventWithTags("トラブル")));
            Assert.AreEqual(CutInStyle.SlideUp,    CutInStyleMap.PickEvent(EventWithTags("研究")));
            Assert.AreEqual(CutInStyle.SlideLeft,  CutInStyleMap.PickEvent(EventWithTags("教授")));
            Assert.AreEqual(CutInStyle.SlideRight, CutInStyleMap.PickEvent(EventWithTags("バイト")));
            Assert.AreEqual(CutInStyle.FlipIn,     CutInStyleMap.PickEvent(EventWithTags("後輩")));
            Assert.AreEqual(CutInStyle.Spin,       CutInStyleMap.PickEvent(EventWithTags("金運")));
            Assert.AreEqual(CutInStyle.FloatIn,    CutInStyleMap.PickEvent(EventWithTags("恋愛")));
        }

        [Test]
        public void Banner_SituationsMapToExpectedStyles()
        {
            Assert.AreEqual(CutInStyle.SlideLeft,  CutInStyleMap.Banner(BannerSituation.TurnStart));
            Assert.AreEqual(CutInStyle.FloatIn,    CutInStyleMap.Banner(BannerSituation.RollPrompt));
            Assert.AreEqual(CutInStyle.SlideRight, CutInStyleMap.Banner(BannerSituation.Moving));
            Assert.AreEqual(CutInStyle.Fade,       CutInStyleMap.Banner(BannerSituation.MassCheck));
            Assert.AreEqual(CutInStyle.SlideUp,    CutInStyleMap.Banner(BannerSituation.GoodEffect));
            Assert.AreEqual(CutInStyle.SlideDown,  CutInStyleMap.Banner(BannerSituation.BadEffect));
            Assert.AreEqual(CutInStyle.CenterPop,  CutInStyleMap.Banner(BannerSituation.EventLabel));
            Assert.AreEqual(CutInStyle.Fade,       CutInStyleMap.Banner(BannerSituation.Generic));
        }

        private static EventMaster EventWithTags(params string[] tags)
        {
            return new EventMaster { Tags = tags, Title = "テスト" };
        }
    }
}
