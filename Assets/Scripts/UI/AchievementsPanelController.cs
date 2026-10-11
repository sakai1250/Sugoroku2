using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Sugoroku.Data;

namespace Sugoroku.UI
{
    /// <summary>タイトル画面の実績パネルの中身を描画する。「実績一覧」「進路図鑑」の2タブを持つ。</summary>
    public class AchievementsPanelController : MonoBehaviour
    {
        private enum Tab { Achievements, CareerGallery }

        private TextMeshProUGUI _bodyText;
        private Button _achievementsTabButton;
        private Button _galleryTabButton;
        private Tab _currentTab = Tab.Achievements;
        private RectTransform _scrollRt;
        private ScrollRect _scroll;

        /// <summary>パネル表示時のキーボードフォーカス初期位置（WCAG 2.4.3）。</summary>
        public Button FirstTabButton => _achievementsTabButton;

        public void Refresh()
        {
            _bodyText ??= transform.Find("AchBody")?.GetComponent<TextMeshProUGUI>();
            if (_bodyText == null) return;

            EnsureTabButtons();
            ApplySafeLayout();
            RenderCurrentTab();
        }

        private void EnsureTabButtons()
        {
            if (_achievementsTabButton != null && _galleryTabButton != null) return;

            _achievementsTabButton = FindOrCreateTabButton("AchTabButton_Achievements", "実績一覧", new Vector2(-90f, 170f));
            _achievementsTabButton.onClick.RemoveAllListeners();
            _achievementsTabButton.onClick.AddListener(() => { _currentTab = Tab.Achievements; RenderCurrentTab(); });

            _galleryTabButton = FindOrCreateTabButton("AchTabButton_Gallery", "進路図鑑", new Vector2(90f, 170f));
            _galleryTabButton.onClick.RemoveAllListeners();
            _galleryTabButton.onClick.AddListener(() => { _currentTab = Tab.CareerGallery; RenderCurrentTab(); });
        }

        private Button FindOrCreateTabButton(string name, string label, Vector2 pos)
        {
            var existing = transform.Find(name);
            if (existing != null)
                return existing.GetComponent<Button>() ?? existing.gameObject.AddComponent<Button>();

            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(transform, false);
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(160f, 44f);
            rt.anchoredPosition = pos;

            var labelGo = new GameObject("Label", typeof(RectTransform));
            labelGo.transform.SetParent(go.transform, false);
            var tmp = labelGo.AddComponent<TextMeshProUGUI>();
            tmp.text = label;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontSize = 20f;
            var labelRt = tmp.rectTransform;
            labelRt.anchorMin = Vector2.zero;
            labelRt.anchorMax = Vector2.one;
            labelRt.offsetMin = labelRt.offsetMax = Vector2.zero;
            JapaneseFontProvider.Apply(tmp);

            var btnComp = go.GetComponent<Button>();
            GameUiChrome.ApplyButton(btnComp, primary: false);
            return btnComp;
        }

        private void RenderCurrentTab()
        {
            _bodyText.text = _currentTab == Tab.Achievements ? BuildAchievementsText() : BuildGalleryText();
            JapaneseFontProvider.Apply(_bodyText);
            UpdateTabHighlight();
        }

        private void UpdateTabHighlight()
        {
            SetTabHighlight(_achievementsTabButton, _currentTab == Tab.Achievements);
            SetTabHighlight(_galleryTabButton, _currentTab == Tab.CareerGallery);
        }

        private static void SetTabHighlight(Button button, bool active)
        {
            if (button == null) return;
            var img = button.GetComponent<Image>();
            if (img != null)
                img.color = active
                    ? new Color(0.25f, 0.30f, 0.42f, 0.98f)
                    : new Color(0.12f, 0.14f, 0.20f, 0.94f);
            var tmp = button.GetComponentInChildren<TextMeshProUGUI>(true);
            if (tmp != null)
                tmp.color = active ? new Color(1f, 0.94f, 0.70f, 1f) : Color.white;

            // アクティブタブだけ下端にゴールド帯を付ける（色以外のキューで判別・WCAG 1.4.1）
            var rule = button.transform.Find("ActiveRule");
            if (active && rule == null)
            {
                var go = new GameObject("ActiveRule", typeof(RectTransform), typeof(Image));
                go.transform.SetParent(button.transform, false);
                var goImg = go.GetComponent<Image>();
                goImg.color = new Color(0.86f, 0.68f, 0.28f, 0.95f);
                goImg.raycastTarget = false;
                var goRt = go.GetComponent<RectTransform>();
                goRt.anchorMin = new Vector2(0f, 0f);
                goRt.anchorMax = new Vector2(1f, 0f);
                goRt.pivot = new Vector2(0.5f, 0f);
                goRt.anchoredPosition = Vector2.zero;
                goRt.sizeDelta = new Vector2(-16f, 4f);
            }
            else if (rule != null)
                rule.gameObject.SetActive(active);
        }

        private void ApplySafeLayout()
        {
            PlaceTab(_achievementsTabButton, -92f);
            PlaceTab(_galleryTabButton, 92f);

            if (_bodyText != null)
            {
                EnsureScrollContainer();
                var rt = _bodyText.rectTransform;
                rt.anchorMin = new Vector2(0f, 1f);
                rt.anchorMax = new Vector2(1f, 1f);
                rt.pivot = new Vector2(0.5f, 1f);
                rt.anchoredPosition = Vector2.zero;
                _bodyText.textWrappingMode = TextWrappingModes.Normal;
                _bodyText.overflowMode = TextOverflowModes.Overflow;
            }

            UiSafeLayout.LayoutCloseButton(transform, transform.Find("CloseButton"));
        }

        /// <summary>実績本文をスクロール可能なビューポートにラップする（長い一覧の末尾切捨て防止）。</summary>
        private void EnsureScrollContainer()
        {
            if (_scrollRt != null) return;

            var existing = transform.Find("AchScrollView");
            if (existing != null)
            {
                _scrollRt = existing as RectTransform;
                _scroll = existing.GetComponent<ScrollRect>();
                if (_scroll == null) _scroll = existing.gameObject.AddComponent<ScrollRect>();
            }
            else
            {
                var go = new GameObject("AchScrollView", typeof(RectTransform), typeof(Image), typeof(RectMask2D), typeof(ScrollRect));
                go.transform.SetParent(transform, false);
                _scrollRt = go.GetComponent<RectTransform>();
                _scrollRt.anchorMin = Vector2.zero;
                _scrollRt.anchorMax = Vector2.one;
                _scrollRt.offsetMin = new Vector2(UiSafeLayout.OuterMargin, 84f);
                _scrollRt.offsetMax = new Vector2(-UiSafeLayout.OuterMargin, -112f);
                var img = go.GetComponent<Image>();
                img.color = new Color(0f, 0f, 0f, 0f);
                img.raycastTarget = false;

                var bodyRt = _bodyText.rectTransform;
                bodyRt.SetParent(_scrollRt, false);
                bodyRt.gameObject.AddComponent<ContentSizeFitter>().verticalFit =
                    ContentSizeFitter.FitMode.PreferredSize;

                _scroll = go.GetComponent<ScrollRect>();
                _scroll.content = bodyRt;
                _scroll.viewport = _scrollRt;
                _scroll.horizontal = false;
                _scroll.vertical = true;
                _scroll.movementType = ScrollRect.MovementType.Clamped;
                _scroll.scrollSensitivity = 30f;

                UiSafeLayout.CreateScrollbar(_scroll, _scrollRt,
                    new Color(0.86f, 0.68f, 0.28f, 0.55f));
            }
        }

        private static void PlaceTab(Button button, float x)
        {
            if (button == null) return;
            var rt = button.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(x, -76f);
            rt.sizeDelta = new Vector2(172f, UiSafeLayout.MinimumButtonHeight);
        }

        private string BuildAchievementsText()
        {
            var sb = new StringBuilder();
            foreach (var def in AchievementCatalog.All)
            {
                bool unlocked = AchievementStore.IsUnlocked(def.Id);
                string mark = unlocked ? "✓" : "・";
                string title = unlocked ? def.Title : $"未達成（{def.Description}）";
                sb.AppendLine($"{mark} {title}");
            }
            return sb.ToString().TrimEnd();
        }

        private string BuildGalleryText()
        {
            var all = CareerOutcomeCatalog.EnumerateAll().ToList();
            int seenCount = all.Count(e => AchievementStore.IsCareerOutcomeSeen(e.Character, e.Rank));

            var sb = new StringBuilder();
            sb.AppendLine($"見た進路 {seenCount}/{all.Count}");
            sb.AppendLine();

            foreach (var entry in all)
            {
                bool seen = AchievementStore.IsCareerOutcomeSeen(entry.Character, entry.Rank);
                string label = $"{entry.Character.DisplayName()} × ランク{entry.Rank}";
                string line = seen
                    ? $"✓ {label}: {entry.CareerPath} — {entry.Subtitle}"
                    : $"・？？？";
                sb.AppendLine(line);
            }

            return sb.ToString().TrimEnd();
        }
    }
}
