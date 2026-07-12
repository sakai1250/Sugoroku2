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
        }

        private void ApplySafeLayout()
        {
            PlaceTab(_achievementsTabButton, -92f);
            PlaceTab(_galleryTabButton, 92f);

            if (_bodyText != null)
            {
                var rt = _bodyText.rectTransform;
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = new Vector2(UiSafeLayout.OuterMargin, 84f);
                rt.offsetMax = new Vector2(-UiSafeLayout.OuterMargin, -112f);
                _bodyText.textWrappingMode = TextWrappingModes.Normal;
                _bodyText.overflowMode = TextOverflowModes.Ellipsis;
            }

            UiSafeLayout.LayoutCloseButton(transform, transform.Find("CloseButton"));
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
                string title = unlocked ? def.Title : $"？？？（{def.Description}）";
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
                    : $"・？？？（条件: {label}）";
                sb.AppendLine(line);
            }

            return sb.ToString().TrimEnd();
        }
    }
}
