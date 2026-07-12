using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using Sugoroku.UI;

namespace Sugoroku.Tests.UI
{
    public class UiSafeLayoutTests
    {
        [Test]
        public void BoardViewportRect_ReservesTopLeftAndBottomRegions()
        {
            Rect rect = UiSafeLayout.BoardViewportRect;

            Assert.That(rect.x, Is.EqualTo(340f / 1920f).Within(0.0001f));
            Assert.That(rect.y, Is.EqualTo(116f / 1080f).Within(0.0001f));
            Assert.That(rect.xMax, Is.EqualTo(1f).Within(0.0001f));
            Assert.That(rect.yMax, Is.EqualTo(1f - 92f / 1080f).Within(0.0001f));
        }

        [Test]
        public void LayoutCloseButton_UsesDedicatedTopRightHitArea()
        {
            var panel = new GameObject("Panel", typeof(RectTransform));
            var close = new GameObject("CloseButton", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            close.transform.SetParent(panel.transform, false);

            UiSafeLayout.LayoutCloseButton(panel.transform, close.transform);

            var rt = close.GetComponent<RectTransform>();
            Assert.That(rt.anchorMin, Is.EqualTo(Vector2.one));
            Assert.That(rt.anchorMax, Is.EqualTo(Vector2.one));
            Assert.That(rt.sizeDelta, Is.EqualTo(new Vector2(48f, 48f)));
            Assert.That(rt.anchoredPosition, Is.EqualTo(new Vector2(-14f, -14f)));
            Assert.That(close.GetComponent<LayoutElement>().ignoreLayout, Is.True);
            Assert.That(close.transform.GetSiblingIndex(), Is.EqualTo(panel.transform.childCount - 1));

            Object.DestroyImmediate(panel);
        }
    }
}
