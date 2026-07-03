using System.Text;
using UnityEngine;
using TMPro;
using Sugoroku.Data;

namespace Sugoroku.UI
{
    /// <summary>タイトル画面の実績パネルの中身を描画する。</summary>
    public class AchievementsPanelController : MonoBehaviour
    {
        private TextMeshProUGUI _bodyText;

        public void Refresh()
        {
            _bodyText ??= transform.Find("AchBody")?.GetComponent<TextMeshProUGUI>();
            if (_bodyText == null) return;

            var sb = new StringBuilder();
            foreach (var def in AchievementCatalog.All)
            {
                bool unlocked = AchievementStore.IsUnlocked(def.Id);
                string mark = unlocked ? "✓" : "・";
                string title = unlocked ? def.Title : $"？？？（{def.Description}）";
                sb.AppendLine($"{mark} {title}");
            }

            _bodyText.text = sb.ToString().TrimEnd();
            JapaneseFontProvider.Apply(_bodyText);
        }
    }
}
