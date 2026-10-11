using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Sugoroku.Data;
using Sugoroku.Game;

namespace Sugoroku.UI
{
    /// <summary>
    /// 画面上部に現在のシーズン（ステージ）とルールバフを常時表示し、
    /// シーズン開幕・クリア時に演出カットインを行うコンポーネント。
    /// </summary>
    public class SeasonHudView : MonoBehaviour
    {
        public static SeasonHudView Instance { get; private set; }

        private RectTransform _badgeRt;
        private Image         _badgeBg;
        private Image         _accentBorder;
        private TextMeshProUGUI _stageText;
        private TextMeshProUGUI _ruleText;

        // カットイン用
        private GameObject    _cutInRoot;
        private CanvasGroup   _cutInGroup;
        private TextMeshProUGUI _cutInTitle;
        private TextMeshProUGUI _cutInDesc;
        private Coroutine     _cutInRoutine;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            BuildBadgeUi();
            BuildCutInUi();
        }

        private void OnEnable()
        {
            SeasonManager.OnSeasonChanged += HandleSeasonChanged;
            SeasonManager.OnSeasonStageCleared += HandleStageCleared;
            UpdateDisplay(SeasonManager.CurrentConfig);
        }

        private void OnDisable()
        {
            SeasonManager.OnSeasonChanged -= HandleSeasonChanged;
            SeasonManager.OnSeasonStageCleared -= HandleStageCleared;
        }

        private void Start()
        {
            UpdateDisplay(SeasonManager.CurrentConfig);
            // ゲーム開始時のシーズン開幕演出
            PlayCutIn(SeasonManager.CurrentConfig, isOpening: true);
        }

        private void BuildBadgeUi()
        {
            var root = new GameObject("SeasonBadgeRoot", typeof(RectTransform));
            root.transform.SetParent(transform, false);
            _badgeRt = root.GetComponent<RectTransform>();
            _badgeRt.anchorMin = new Vector2(0.5f, 1f);
            _badgeRt.anchorMax = new Vector2(0.5f, 1f);
            _badgeRt.pivot     = new Vector2(0.5f, 1f);
            // ResourceBar の下、GameStatusBannerの上に配置（重なり防止）
            _badgeRt.anchoredPosition = new Vector2(0f, -(UiSafeLayout.TopBarHeight + 6f));
            _badgeRt.sizeDelta = new Vector2(460f, 40f);

            _badgeBg = root.AddComponent<Image>();
            _badgeBg.color = new Color(0.10f, 0.13f, 0.18f, 0.95f);
            _badgeBg.raycastTarget = false;

            // 枠線
            var borderGo = new GameObject("Border", typeof(RectTransform), typeof(Image));
            borderGo.transform.SetParent(root.transform, false);
            var borderRt = borderGo.GetComponent<RectTransform>();
            borderRt.anchorMin = Vector2.zero;
            borderRt.anchorMax = Vector2.one;
            borderRt.sizeDelta = Vector2.zero;
            _accentBorder = borderGo.GetComponent<Image>();
            _accentBorder.color = new Color(0.2f, 0.65f, 1f, 0.8f);
            _accentBorder.raycastTarget = false;

            // ステージタイトル
            var titleGo = new GameObject("StageTitle", typeof(RectTransform), typeof(TextMeshProUGUI));
            titleGo.transform.SetParent(root.transform, false);
            var titleRt = titleGo.GetComponent<RectTransform>();
            titleRt.anchorMin = new Vector2(0f, 0.5f);
            titleRt.anchorMax = new Vector2(0.48f, 1f);
            titleRt.offsetMin = new Vector2(10f, 0f);
            titleRt.offsetMax = new Vector2(0f, -4f);
            _stageText = titleGo.GetComponent<TextMeshProUGUI>();
            _stageText.alignment = TextAlignmentOptions.MidlineLeft;
            _stageText.fontSize = 18f;
            _stageText.fontStyle = FontStyles.Bold;
            _stageText.color = Color.white;
            HudTextStyle.ApplyOutlineSafe(_stageText, 0.2f, Color.black);

            // ルール概要
            var ruleGo = new GameObject("RuleDesc", typeof(RectTransform), typeof(TextMeshProUGUI));
            ruleGo.transform.SetParent(root.transform, false);
            var ruleRt = ruleGo.GetComponent<RectTransform>();
            ruleRt.anchorMin = new Vector2(0.48f, 0f);
            ruleRt.anchorMax = new Vector2(1f, 1f);
            ruleRt.offsetMin = new Vector2(0f, 2f);
            ruleRt.offsetMax = new Vector2(-10f, -2f);
            _ruleText = ruleGo.GetComponent<TextMeshProUGUI>();
            _ruleText.alignment = TextAlignmentOptions.MidlineRight;
            _ruleText.fontSize = 13f;
            _ruleText.color = new Color(1f, 0.92f, 0.65f, 1f);
            _ruleText.enableAutoSizing = true;
            _ruleText.fontSizeMin = 10f;
            _ruleText.fontSizeMax = 14f;
        }

        private void BuildCutInUi()
        {
            _cutInRoot = new GameObject("SeasonCutInOverlay", typeof(RectTransform));
            _cutInRoot.transform.SetParent(transform, false);
            var rt = _cutInRoot.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero;

            _cutInGroup = _cutInRoot.AddComponent<CanvasGroup>();
            _cutInGroup.alpha = 0f;
            _cutInGroup.blocksRaycasts = false;

            // 背景ダーク幕
            var bgGo = new GameObject("DimBg", typeof(RectTransform), typeof(Image));
            bgGo.transform.SetParent(_cutInRoot.transform, false);
            var bgRt = bgGo.GetComponent<RectTransform>();
            bgRt.anchorMin = new Vector2(0f, 0.38f);
            bgRt.anchorMax = new Vector2(1f, 0.62f);
            bgRt.sizeDelta = Vector2.zero;
            var bgImg = bgGo.GetComponent<Image>();
            bgImg.color = new Color(0.05f, 0.08f, 0.12f, 0.92f);
            bgImg.raycastTarget = false;

            // タイトル
            var tGo = new GameObject("CutInTitle", typeof(RectTransform), typeof(TextMeshProUGUI));
            tGo.transform.SetParent(bgGo.transform, false);
            var tRt = tGo.GetComponent<RectTransform>();
            tRt.anchorMin = new Vector2(0f, 0.5f);
            tRt.anchorMax = new Vector2(1f, 1f);
            tRt.offsetMin = new Vector2(20f, 0f);
            tRt.offsetMax = new Vector2(-20f, -10f);
            _cutInTitle = tGo.GetComponent<TextMeshProUGUI>();
            _cutInTitle.alignment = TextAlignmentOptions.Center;
            _cutInTitle.fontSize = 36f;
            _cutInTitle.fontStyle = FontStyles.Bold;
            _cutInTitle.color = Color.white;
            HudTextStyle.ApplyOutlineSafe(_cutInTitle, 0.3f, Color.black);

            // 説明
            var dGo = new GameObject("CutInDesc", typeof(RectTransform), typeof(TextMeshProUGUI));
            dGo.transform.SetParent(bgGo.transform, false);
            var dRt = dGo.GetComponent<RectTransform>();
            dRt.anchorMin = new Vector2(0f, 0f);
            dRt.anchorMax = new Vector2(1f, 0.5f);
            dRt.offsetMin = new Vector2(30f, 10f);
            dRt.offsetMax = new Vector2(-30f, 0f);
            _cutInDesc = dGo.GetComponent<TextMeshProUGUI>();
            _cutInDesc.alignment = TextAlignmentOptions.Center;
            _cutInDesc.fontSize = 20f;
            _cutInDesc.color = new Color(1f, 0.92f, 0.65f, 1f);
            HudTextStyle.ApplyOutlineSafe(_cutInDesc, 0.2f, Color.black);
        }

        public void UpdateDisplay(SeasonConfig config)
        {
            if (config == null) return;
            if (_stageText != null)
            {
                string icon = config.Type switch
                {
                    SeasonType.ResearchWriting => "🔬",
                    SeasonType.Conference      => "✈️",
                    SeasonType.PartTime        => "💼",
                    _                          => "🎓"
                };
                _stageText.text = $"{icon} Stage {config.StageNumber}: {config.Title}";
                _stageText.color = config.ThemeColor;
            }

            if (_ruleText != null)
            {
                string briefRule = config.Type switch
                {
                    SeasonType.ResearchWriting => "IF+50% / メンタル消費+20%",
                    SeasonType.Conference      => "所持金消費+50% / IF+10pt / 徳2倍",
                    SeasonType.PartTime        => "所持金+50% / メンタル回復+50%",
                    _                          => ""
                };
                _ruleText.text = briefRule;
            }

            if (_accentBorder != null)
            {
                _accentBorder.color = config.ThemeColor;
            }
        }

        private void HandleSeasonChanged(SeasonConfig config)
        {
            UpdateDisplay(config);
            PlayCutIn(config, isOpening: true);
        }

        private void HandleStageCleared(SeasonConfig config)
        {
            PlayCutIn(config, isOpening: false);
        }

        public void PlayCutIn(SeasonConfig config, bool isOpening)
        {
            if (_cutInRoutine != null) StopCoroutine(_cutInRoutine);
            _cutInRoutine = StartCoroutine(CutInCoroutine(config, isOpening));
        }

        private IEnumerator CutInCoroutine(SeasonConfig config, bool isOpening)
        {
            if (_cutInGroup == null || config == null) yield break;

            if (isOpening)
            {
                _cutInTitle.text = $"◆ Stage {config.StageNumber}: {config.Title} 開幕 ◆";
                _cutInTitle.color = config.ThemeColor;
                _cutInDesc.text = config.Description;
            }
            else
            {
                _cutInTitle.text = $"★ Stage {config.StageNumber}: {config.Title} クリア！ ★";
                _cutInTitle.color = Color.yellow;
                _cutInDesc.text = "シーズン総括ボーナス獲得！ 次のステージへ進みます！";
            }

            // フェードイン
            float elapsed = 0f;
            float fadeInDur = 0.35f;
            while (elapsed < fadeInDur)
            {
                elapsed += Time.unscaledDeltaTime;
                _cutInGroup.alpha = Mathf.Clamp01(elapsed / fadeInDur);
                yield return null;
            }
            _cutInGroup.alpha = 1f;

            // 表示待機
            yield return new WaitForSecondsRealtime(1.8f);

            // フェードアウト
            elapsed = 0f;
            float fadeOutDur = 0.4f;
            while (elapsed < fadeOutDur)
            {
                elapsed += Time.unscaledDeltaTime;
                _cutInGroup.alpha = Mathf.Clamp01(1f - (elapsed / fadeOutDur));
                yield return null;
            }
            _cutInGroup.alpha = 0f;
            _cutInRoutine = null;
        }
    }
}
