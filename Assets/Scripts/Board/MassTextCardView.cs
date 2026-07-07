using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Sugoroku.Data;
using Sugoroku.UI;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

namespace Sugoroku.Board
{
    /// <summary>
    /// 横長カード型マス（World Space UI）。EventId から events.json のタイトル・タグを表示。
    /// タップで詳細（説明文）を表示。
    /// </summary>
    [DisallowMultipleComponent]
    public class MassTextCardView : MonoBehaviour
    {
        [SerializeField] private string eventId;
        [SerializeField] private SquareType squareType = SquareType.Normal;
        [SerializeField] private string fallbackTitle = "";

        [Header("UI")]
        [SerializeField] private Image           background;
        [SerializeField] private Image           massArtImage;
        [SerializeField] private Image           border;
        [SerializeField] private Image           accentStrip;
        [SerializeField] private Image           headerBand;
        [SerializeField] private Image           depthShadow;
        [SerializeField] private Image           bottomExtrude;
        [SerializeField] private Image           rightExtrude;
        [SerializeField] private Image           outerGlow;
        [SerializeField] private Image           hoverWash;
        [SerializeField] private Image           shineSweep;
        [SerializeField] private Image           cornerMarker;
        [SerializeField] private Image           titleRule;
        [SerializeField] private Image           pixelGroundLip;
        [SerializeField] private Image           dirtChipLeft;
        [SerializeField] private Image           dirtChipRight;
        [SerializeField] private Image           terrainBlockLeft;
        [SerializeField] private Image           terrainBlockMid;
        [SerializeField] private Image           terrainBlockRight;
        [SerializeField] private Image           coinPipTop;
        [SerializeField] private Image           coinPipMid;
        [SerializeField] private Image           coinPipLow;
        [SerializeField] private Image           sparkleTop;
        [SerializeField] private Image           sparkleMid;
        [SerializeField] private Image           sparkleLow;
        [SerializeField] private TextMeshProUGUI tagText;
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI markerText;
        [SerializeField] private BoxCollider2D   hitCollider;

        [Header("Pixel Style")]
        [SerializeField] private bool pixelBlockStyle = true;

        [Header("Motion")]
        [SerializeField] private bool  animateCard        = true;
        [SerializeField] private float hoverScale         = 1.055f;
        [SerializeField] private float hoverBlendSeconds  = 0.12f;
        [SerializeField] private float idleFloatAmplitude = 0.018f;
        [SerializeField] private float idleFloatSpeed     = 1.45f;
        [SerializeField] private float clickPunchScale    = 0.045f;
        [SerializeField] private float clickPunchSeconds  = 0.22f;
        [SerializeField] private float shineCycleSpeed    = 0.12f;
        [SerializeField] private float sparkleSpeed       = 0.72f;
        [SerializeField] private float maxTiltDegrees     = 7.5f;
        [SerializeField] private float hoverLiftPixels    = 4.5f;

        public string EventId => eventId;

        public void PlayEventHighlight()
        {
            if (!isActiveAndEnabled) return;
            StopAllCoroutines();
            StartCoroutine(EventHighlightCoroutine());
        }

        private IEnumerator EventHighlightCoroutine()
        {
            CaptureMotionBaseline();
            if (_motionRoot == null) yield break;

            float duration = GameConfig.AnimationDuration(0.55f);
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float pulse = 1f + Mathf.Sin(t * Mathf.PI) * 0.14f;
                _motionRoot.localScale = _motionRootBaseScale * pulse;
                yield return null;
            }

            ResetMotionTransform();
        }

        private Canvas  _cardCanvas;
        private Transform _motionRoot;
        private Vector3 _motionRootBaseScale = Vector3.one;
        private Vector3 _motionRootBaseLocalPosition = Vector3.zero;
        private Quaternion _motionRootBaseLocalRotation = Quaternion.identity;
        private bool    _motionBaselineCaptured;
        private bool    _isHovered;
        private Vector2 _hoverLocalNormalized;
        private float   _hoverAmount;
        private float   _clickPunch;
        private float   _motionSeed;
        private float   _shineSeed;
        private Color   _panelColor  = new(0.42f, 0.46f, 0.56f, 0.94f);
        private Color   _accentColor = new(0.58f, 0.62f, 0.72f, 1f);
        private bool    _massArtActive;

        private void Awake()
        {
            int id = Mathf.Abs(GetInstanceID() % 1024);
            _motionSeed = id * 0.173f;
            _shineSeed  = Mathf.Repeat(id * 0.037f, 1f);
            CacheReferences();
            CaptureMotionBaseline();
        }

        private void OnDisable()
        {
            _isHovered = false;
            _hoverAmount = 0f;
            _clickPunch = 0f;
            ResetMotionTransform();
        }

        public void Configure(SquareType type, string boundEventId, string titleFallback)
        {
            squareType     = type;
            eventId        = boundEventId;
            fallbackTitle  = titleFallback ?? "";
            RefreshDisplay();
        }

        public void RefreshDisplay()
        {
            CacheReferences();

            if (!string.IsNullOrEmpty(eventId))
            {
                var ev = EventMassCatalog.Get(eventId);
                if (ev != null)
                {
                    ApplyEvent(ev);
                    return;
                }
            }

            ApplySquareType(squareType, fallbackTitle);
        }

        private void ApplyEvent(EventMaster ev)
        {
            if (tagText != null)
            {
                tagText.text = EventTagColors.FormatTags(ev.Tags);
                tagText.color = new Color(0.92f, 0.94f, 1f, 0.95f);
            }

            if (titleText != null)
            {
                titleText.text = ev.Title ?? "";
                titleText.color = Color.white;
            }
            if (markerText != null)
                markerText.text = squareType == SquareType.Branch ? "分岐" : "EV";

            var panel  = squareType == SquareType.Branch
                ? EventTagColors.GetSquareTypePanelColor(SquareType.Branch)
                : EventTagColors.GetPanelColor(ev.Tags);
            var accent = squareType == SquareType.Branch
                ? EventTagColors.GetSquareTypePanelColor(SquareType.Branch)
                : EventTagColors.GetBorderColor(ev.Tags);
            if (background != null)
                background.color = panel;
            if (border != null)
                border.color = accent;
            ApplyCardDecoration(panel, accent);
            ApplyMassArt(squareType, ev.Tags);
        }

        private void ApplySquareType(SquareType type, string title)
        {
            string tag = type switch
            {
                SquareType.Start   => "スタート",
                SquareType.Goal    => "ゴール",
                SquareType.Tuition => "学費",
                SquareType.Journal => "論文",
                SquareType.Lecture => "ゼミ",
                SquareType.Rest     => "休息",
                SquareType.PartTime => "バイト",
                SquareType.Bonus    => "チャンス",
                SquareType.Penalty => "ペナルティ",
                SquareType.Branch  => "分岐",
                SquareType.Event   => "イベント",
                _                  => "通常",
            };

            if (tagText != null)
            {
                tagText.text  = $"[{tag}]";
                tagText.color = new Color(0.9f, 0.92f, 0.96f, 0.9f);
            }

            if (titleText != null)
            {
                titleText.text  = string.IsNullOrEmpty(title) ? tag : title;
                titleText.color = Color.white;
                titleText.fontSize = Mathf.Max(titleText.fontSize, 14f);
            }
            if (markerText != null)
                markerText.text = Waypoint.GetTypeShortLabel(type);

            var panel = EventTagColors.GetSquareTypePanelColor(type);
            if (background != null) background.color = panel;
            var accent = new Color(
                Mathf.Min(panel.r * 1.2f, 1f),
                Mathf.Min(panel.g * 1.2f, 1f),
                Mathf.Min(panel.b * 1.2f, 1f),
                1f);
            if (border != null)
                border.color = accent;
            ApplyCardDecoration(panel, accent);
            ApplyMassArt(type, null);
        }

        private void ApplyMassArt(SquareType type, string[] tags)
        {
            EnsureMassArtImage();

            var category = EventMasuArt.ResolveCategory(type, tags);
            var sprite   = EventMasuArt.GetCardSprite(category);

            if (massArtImage == null)
            {
                SetMassArtPresentation(false);
                return;
            }

            if (sprite == null)
            {
                MassTextCardArtLayout.ApplySprite(massArtImage, null);
                SetMassArtPresentation(false);
                return;
            }

            MassTextCardArtLayout.ApplySprite(massArtImage, sprite);
            SetMassArtPresentation(true);

            if (background != null)
                background.color = new Color(0.06f, 0.07f, 0.09f, 1f);
        }

        private void SetMassArtPresentation(bool artActive)
        {
            _massArtActive = artActive;

            SetGraphicVisible(pixelGroundLip, !artActive);
            SetGraphicVisible(dirtChipLeft, !artActive);
            SetGraphicVisible(dirtChipRight, !artActive);
            SetGraphicVisible(terrainBlockLeft, !artActive);
            SetGraphicVisible(terrainBlockMid, !artActive);
            SetGraphicVisible(terrainBlockRight, !artActive);
            SetGraphicVisible(coinPipTop, !artActive);
            SetGraphicVisible(coinPipMid, !artActive);
            SetGraphicVisible(coinPipLow, !artActive);
            SetGraphicVisible(sparkleTop, !artActive);
            SetGraphicVisible(sparkleMid, !artActive);
            SetGraphicVisible(sparkleLow, !artActive);
            SetGraphicVisible(titleRule, !artActive);
            SetGraphicVisible(shineSweep, !artActive);

            if (headerBand != null)
            {
                headerBand.enabled = true;
                if (artActive)
                {
                    SetRect(headerBand, new Vector2(0f, 0f), new Vector2(1f, 0f),
                        new Vector2(0.5f, 0f), new Vector2(0f, 22f), new Vector2(0f, 11f));
                    headerBand.color = new Color(0f, 0f, 0f, 0.72f);
                }
                else
                {
                    SetRect(headerBand, new Vector2(0f, 1f), new Vector2(1f, 1f),
                        new Vector2(0.5f, 1f), new Vector2(0f, 25f), new Vector2(0f, -12.5f));
                }
            }

            EnsureMassArtSiblingOrder();

            if (artActive)
                LayoutMassArtCaption();
            else
                LayoutText();
        }

        private void EnsureMassArtSiblingOrder()
        {
            if (background == null) return;

            var panel = background.transform;
            var clip = panel.Find(MassTextCardArtLayout.ClipName);
            if (clip == null) return;

            clip.SetAsFirstSibling();
            if (accentStrip != null)
                accentStrip.transform.SetSiblingIndex(1);
        }

        private void LayoutMassArtCaption()
        {
            if (tagText != null)
            {
                var rt = tagText.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0f, 0f);
                rt.anchorMax = new Vector2(1f, 0f);
                rt.pivot = new Vector2(0.5f, 0f);
                rt.anchoredPosition = new Vector2(14f, 2f);
                rt.sizeDelta = new Vector2(-28f, 20f);
                tagText.alignment = TextAlignmentOptions.BottomLeft;
                tagText.enableAutoSizing = true;
                tagText.fontSizeMin = 8f;
                tagText.fontSizeMax = 11f;
                HudTextStyle.ApplyOutlineSafe(tagText, 0.08f, new Color(0f, 0f, 0f, 0.62f));
            }

            if (titleText != null)
            {
                var rt = titleText.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0f, 0f);
                rt.anchorMax = new Vector2(1f, 0f);
                rt.pivot = new Vector2(0.5f, 0f);
                rt.offsetMin = new Vector2(14f, 18f);
                rt.offsetMax = new Vector2(-34f, 42f);
                titleText.alignment = TextAlignmentOptions.BottomLeft;
                titleText.enableAutoSizing = true;
                titleText.fontSizeMin = 10f;
                titleText.fontSizeMax = 14f;
                HudTextStyle.ApplyOutlineSafe(titleText, 0.10f, new Color(0f, 0f, 0f, 0.68f));
            }
        }

        private static void SetGraphicVisible(Graphic graphic, bool visible)
        {
            if (graphic != null)
                graphic.enabled = visible;
        }

        private void EnsureMassArtImage()
        {
            var panel = background != null
                ? background.transform
                : transform.Find("CardCanvas/CardPanel");
            if (panel == null) return;

            massArtImage = MassTextCardArtLayout.EnsureMassArtImage(panel);
        }

        private void Update()
        {
            UpdatePointerState(out bool clickedThisFrame);
            AnimateCard();

            if (!clickedThisFrame || !_isHovered) return;
            PlayClickFeedback();
            ShowDetail();
        }

        private void ShowDetail()
        {
            if (!string.IsNullOrEmpty(eventId))
            {
                var ev = EventMassCatalog.Get(eventId);
                if (ev != null)
                {
                    EventModalUI.ShowPreview(ev);
                    return;
                }
            }

            EventModalUI.ShowSquarePreview(squareType, fallbackTitle,
                "このマスに止まると、ゲーム内のルールに従って効果が発動します。");
        }

        private void CacheReferences()
        {
            hitCollider ??= GetComponent<BoxCollider2D>();
            EnsureCanvasRenders();

            var canvas = GetComponentInChildren<Canvas>(true);
            if (canvas == null) return;

            _cardCanvas = canvas;
            if (_motionRoot != canvas.transform)
            {
                _motionRoot = canvas.transform;
                _motionBaselineCaptured = false;
            }
            CaptureMotionBaseline();

            background ??= canvas.transform.Find("CardPanel")?.GetComponent<Image>();
            massArtImage ??= canvas.transform.Find("CardPanel/MassArtClip/MassArtImage")?.GetComponent<Image>()
                ?? canvas.transform.Find("CardPanel/MassArtImage")?.GetComponent<Image>();
            border     ??= canvas.transform.Find("CardBorder")?.GetComponent<Image>();
            tagText    ??= canvas.transform.Find("CardPanel/TagLabel")?.GetComponent<TextMeshProUGUI>();
            titleText  ??= canvas.transform.Find("CardPanel/TitleLabel")?.GetComponent<TextMeshProUGUI>();
            CacheDecorationReferences();
            ApplyPixelSprites();
        }

        private void CacheDecorationReferences()
        {
            if (background == null) return;

            var panel = background.transform;
            var canvasRoot = _cardCanvas != null ? _cardCanvas.transform : background.transform.parent;
            if (canvasRoot != null)
            {
                depthShadow ??= canvasRoot.Find("CardDepthShadow")?.GetComponent<Image>();
                bottomExtrude ??= canvasRoot.Find("CardBottomExtrude")?.GetComponent<Image>();
                rightExtrude ??= canvasRoot.Find("CardRightExtrude")?.GetComponent<Image>();
                outerGlow ??= canvasRoot.Find("CardGlow")?.GetComponent<Image>();
                depthShadow ??= CreateDecorationImage(canvasRoot, "CardDepthShadow");
                bottomExtrude ??= CreateDecorationImage(canvasRoot, "CardBottomExtrude");
                rightExtrude ??= CreateDecorationImage(canvasRoot, "CardRightExtrude");
                outerGlow ??= CreateDecorationImage(canvasRoot, "CardGlow");
            }

            accentStrip  ??= panel.Find("AccentStrip")?.GetComponent<Image>();
            headerBand   ??= panel.Find("HeaderBand")?.GetComponent<Image>();
            hoverWash    ??= panel.Find("HoverWash")?.GetComponent<Image>();
            shineSweep   ??= panel.Find("ShineSweep")?.GetComponent<Image>();
            cornerMarker ??= panel.Find("CornerMarker")?.GetComponent<Image>();
            titleRule    ??= panel.Find("TitleRule")?.GetComponent<Image>();
            pixelGroundLip ??= panel.Find("PixelGroundLip")?.GetComponent<Image>();
            dirtChipLeft ??= panel.Find("DirtChipLeft")?.GetComponent<Image>();
            dirtChipRight ??= panel.Find("DirtChipRight")?.GetComponent<Image>();
            terrainBlockLeft ??= panel.Find("TerrainBlockLeft")?.GetComponent<Image>();
            terrainBlockMid ??= panel.Find("TerrainBlockMid")?.GetComponent<Image>();
            terrainBlockRight ??= panel.Find("TerrainBlockRight")?.GetComponent<Image>();
            coinPipTop ??= panel.Find("CoinPipTop")?.GetComponent<Image>();
            coinPipMid ??= panel.Find("CoinPipMid")?.GetComponent<Image>();
            coinPipLow ??= panel.Find("CoinPipLow")?.GetComponent<Image>();
            sparkleTop   ??= panel.Find("SparkleTop")?.GetComponent<Image>();
            sparkleMid   ??= panel.Find("SparkleMid")?.GetComponent<Image>();
            sparkleLow   ??= panel.Find("SparkleLow")?.GetComponent<Image>();
            markerText   ??= panel.Find("MarkerLabel")?.GetComponent<TextMeshProUGUI>();

            accentStrip  ??= CreateDecorationImage(panel, "AccentStrip");
            headerBand   ??= CreateDecorationImage(panel, "HeaderBand");
            hoverWash    ??= CreateDecorationImage(panel, "HoverWash");
            shineSweep   ??= CreateDecorationImage(panel, "ShineSweep");
            cornerMarker ??= CreateDecorationImage(panel, "CornerMarker");
            titleRule    ??= CreateDecorationImage(panel, "TitleRule");
            pixelGroundLip ??= CreateDecorationImage(panel, "PixelGroundLip");
            dirtChipLeft ??= CreateDecorationImage(panel, "DirtChipLeft");
            dirtChipRight ??= CreateDecorationImage(panel, "DirtChipRight");
            terrainBlockLeft ??= CreateDecorationImage(panel, "TerrainBlockLeft");
            terrainBlockMid ??= CreateDecorationImage(panel, "TerrainBlockMid");
            terrainBlockRight ??= CreateDecorationImage(panel, "TerrainBlockRight");
            coinPipTop ??= CreateCoinDecorationImage(panel, "CoinPipTop");
            coinPipMid ??= CreateCoinDecorationImage(panel, "CoinPipMid");
            coinPipLow ??= CreateCoinDecorationImage(panel, "CoinPipLow");
            sparkleTop   ??= CreateDotDecorationImage(panel, "SparkleTop");
            sparkleMid   ??= CreateDotDecorationImage(panel, "SparkleMid");
            sparkleLow   ??= CreateDotDecorationImage(panel, "SparkleLow");
            markerText   ??= CreateDecorationText(panel, "MarkerLabel");

            LayoutDecoration();
            LayoutText();
        }

        private void LayoutDecoration()
        {
            SetRect(depthShadow, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), new Vector2(266f, 96f), new Vector2(9f, -9f));
            SetRect(bottomExtrude, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), new Vector2(246f, 11f), new Vector2(5f, -43f));
            SetRect(rightExtrude, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), new Vector2(11f, 78f), new Vector2(128f, -3f));
            SetRect(outerGlow, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), new Vector2(270f, 102f), new Vector2(0f, 4f));
            SetRect(accentStrip, new Vector2(0f, 0f), new Vector2(0f, 1f),
                new Vector2(0f, 0.5f), new Vector2(11f, 0f), new Vector2(5.5f, 0f));
            SetRect(headerBand, new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0.5f, 1f), new Vector2(0f, 25f), new Vector2(0f, -12.5f));
            SetRect(hoverWash, new Vector2(0f, 0f), new Vector2(1f, 1f),
                new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            SetRect(shineSweep, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), new Vector2(28f, 124f), new Vector2(-145f, -3f));
            SetRect(cornerMarker, new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(1f, 1f), new Vector2(28f, 18f), new Vector2(-9f, -8f));
            SetRect(markerText, new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(1f, 1f), new Vector2(28f, 18f), new Vector2(-9f, -8f));
            SetRect(titleRule, new Vector2(0f, 0f), new Vector2(1f, 0f),
                new Vector2(0.5f, 0f), new Vector2(-24f, 3f), new Vector2(8f, 8f));
            SetRect(pixelGroundLip, new Vector2(0f, 0f), new Vector2(1f, 0f),
                new Vector2(0.5f, 0f), new Vector2(-26f, 6f), new Vector2(8f, 4f));
            SetRect(dirtChipLeft, new Vector2(0f, 0f), new Vector2(0f, 0f),
                new Vector2(0.5f, 0.5f), new Vector2(18f, 4f), new Vector2(40f, 14f));
            SetRect(dirtChipRight, new Vector2(1f, 0f), new Vector2(1f, 0f),
                new Vector2(0.5f, 0.5f), new Vector2(22f, 4f), new Vector2(-54f, 14f));
            SetRect(terrainBlockLeft, new Vector2(0f, 0f), new Vector2(0f, 0f),
                new Vector2(0.5f, 0f), new Vector2(18f, 10f), new Vector2(30f, 4f));
            SetRect(terrainBlockMid, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f), new Vector2(24f, 8f), new Vector2(-8f, 4f));
            SetRect(terrainBlockRight, new Vector2(1f, 0f), new Vector2(1f, 0f),
                new Vector2(0.5f, 0f), new Vector2(18f, 10f), new Vector2(-38f, 4f));
            SetRect(coinPipTop, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(0.5f, 0.5f), new Vector2(9f, 9f), new Vector2(5.5f, 24f));
            SetRect(coinPipMid, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(0.5f, 0.5f), new Vector2(9f, 9f), new Vector2(5.5f, 0f));
            SetRect(coinPipLow, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(0.5f, 0.5f), new Vector2(9f, 9f), new Vector2(5.5f, -24f));
            SetRect(sparkleTop, new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(0.5f, 0.5f), new Vector2(8f, 8f), new Vector2(-42f, -32f));
            SetRect(sparkleMid, new Vector2(0f, 0f), new Vector2(0f, 0f),
                new Vector2(0.5f, 0.5f), new Vector2(6f, 6f), new Vector2(36f, 21f));
            SetRect(sparkleLow, new Vector2(1f, 0f), new Vector2(1f, 0f),
                new Vector2(0.5f, 0.5f), new Vector2(5f, 5f), new Vector2(-24f, 18f));

            if (depthShadow != null)
                depthShadow.transform.SetAsFirstSibling();
            if (outerGlow != null)
                outerGlow.transform.SetSiblingIndex(depthShadow != null ? 1 : 0);
            if (bottomExtrude != null)
                bottomExtrude.transform.SetSiblingIndex(outerGlow != null ? outerGlow.transform.GetSiblingIndex() + 1 : 1);
            if (rightExtrude != null)
                rightExtrude.transform.SetSiblingIndex(bottomExtrude != null ? bottomExtrude.transform.GetSiblingIndex() + 1 : 1);

            var cardPanel = background != null ? background.transform : null;
            var artClip = cardPanel != null ? cardPanel.Find(MassTextCardArtLayout.ClipName) : null;
            if (artClip != null)
                artClip.SetAsFirstSibling();
            if (accentStrip != null)
                accentStrip.transform.SetSiblingIndex(artClip != null ? 1 : 0);
            headerBand.transform.SetSiblingIndex(1);
            hoverWash.transform.SetSiblingIndex(2);
            shineSweep.transform.SetSiblingIndex(3);
            cornerMarker.transform.SetSiblingIndex(4);
            titleRule.transform.SetSiblingIndex(5);
            pixelGroundLip.transform.SetSiblingIndex(6);
            dirtChipLeft.transform.SetSiblingIndex(7);
            dirtChipRight.transform.SetSiblingIndex(8);
            terrainBlockLeft.transform.SetSiblingIndex(9);
            terrainBlockMid.transform.SetSiblingIndex(10);
            terrainBlockRight.transform.SetSiblingIndex(11);
            coinPipTop.transform.SetSiblingIndex(12);
            coinPipMid.transform.SetSiblingIndex(13);
            coinPipLow.transform.SetSiblingIndex(14);
            sparkleTop.transform.SetSiblingIndex(15);
            sparkleMid.transform.SetSiblingIndex(16);
            sparkleLow.transform.SetSiblingIndex(17);
            tagText?.transform.SetAsLastSibling();
            titleText?.transform.SetAsLastSibling();
            markerText?.transform.SetAsLastSibling();

            if (shineSweep != null)
                shineSweep.transform.localEulerAngles = new Vector3(0f, 0f, -15f);
        }

        private void LayoutText()
        {
            if (tagText != null)
            {
                var rt = tagText.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0f, 1f);
                rt.anchorMax = new Vector2(1f, 1f);
                rt.pivot = new Vector2(0.5f, 1f);
                rt.anchoredPosition = new Vector2(18f, -5f);
                rt.sizeDelta = new Vector2(-54f, 22f);
                tagText.enableAutoSizing = true;
                tagText.fontSizeMin = 9f;
                tagText.fontSizeMax = 13f;
                HudTextStyle.ApplyOutlineSafe(tagText, 0.08f, new Color(0f, 0f, 0f, 0.62f));
            }

            if (titleText != null)
            {
                var rt = titleText.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0f, 0f);
                rt.anchorMax = new Vector2(1f, 1f);
                rt.offsetMin = new Vector2(18f, 9f);
                rt.offsetMax = new Vector2(-12f, -29f);
                titleText.enableAutoSizing = true;
                titleText.fontSizeMin = 11f;
                titleText.fontSizeMax = 17f;
                HudTextStyle.ApplyOutlineSafe(titleText, 0.10f, new Color(0f, 0f, 0f, 0.68f));
            }

            if (markerText != null)
            {
                markerText.enableAutoSizing = true;
                markerText.fontSizeMin = 7f;
                markerText.fontSizeMax = 10f;
                HudTextStyle.ApplyOutlineSafe(markerText, 0.04f, new Color(1f, 1f, 1f, 0.30f));
            }
        }

        private void ApplyCardDecoration(Color panel, Color accent)
        {
            CacheDecorationReferences();
            if (pixelBlockStyle)
            {
                panel = PixelPanelColor(panel);
                accent = PixelAccentColor(accent);
                if (background != null) background.color = panel;
                if (border != null) border.color = Shade(accent, 0.16f);
            }

            _panelColor = panel;
            _accentColor = accent;

            if (depthShadow != null) depthShadow.color = new Color(0f, 0f, 0f, pixelBlockStyle ? 0.28f : 0.20f);
            if (bottomExtrude != null) bottomExtrude.color = WithAlpha(Shade(panel, 0.42f), 0.96f);
            if (rightExtrude != null) rightExtrude.color = WithAlpha(Shade(panel, 0.50f), 0.94f);
            if (accentStrip != null) accentStrip.color = accent;
            if (headerBand != null)
                headerBand.color = pixelBlockStyle
                    ? WithAlpha(Tint(accent, 0.18f), 0.78f)
                    : WithAlpha(Shade(panel, 0.24f), 0.44f);
            if (outerGlow != null) outerGlow.color = WithAlpha(Tint(accent, 0.22f), 0.11f);
            if (hoverWash != null) hoverWash.color = WithAlpha(Tint(accent, 0.48f), 0.035f);
            if (shineSweep != null) shineSweep.color = WithAlpha(pixelBlockStyle ? new Color(1f, 0.94f, 0.52f) : Color.white, 0.055f);
            if (cornerMarker != null)
                cornerMarker.color = pixelBlockStyle
                    ? new Color(1f, 0.78f, 0.24f, 0.96f)
                    : WithAlpha(Tint(accent, 0.22f), 0.95f);
            if (titleRule != null) titleRule.color = WithAlpha(Tint(accent, 0.36f), 0.92f);
            if (pixelGroundLip != null)
                pixelGroundLip.color = pixelBlockStyle
                    ? WithAlpha(PixelGrassColor(accent), 0.96f)
                    : WithAlpha(Tint(accent, 0.28f), 0.32f);
            if (dirtChipLeft != null) dirtChipLeft.color = WithAlpha(Shade(panel, 0.34f), pixelBlockStyle ? 0.62f : 0.18f);
            if (dirtChipRight != null) dirtChipRight.color = WithAlpha(Shade(panel, 0.40f), pixelBlockStyle ? 0.58f : 0.16f);
            ApplyTerrainBlockColors(panel, accent, 0f);
            ApplyCoinPipColors(accent, 0f);
            if (sparkleTop != null) sparkleTop.color = WithAlpha(Tint(accent, 0.44f), 0.14f);
            if (sparkleMid != null) sparkleMid.color = WithAlpha(Tint(accent, 0.55f), 0.12f);
            if (sparkleLow != null) sparkleLow.color = WithAlpha(Tint(accent, 0.36f), 0.10f);
            if (markerText != null) markerText.color = WithAlpha(Shade(accent, 0.58f), 1f);

            if (_massArtActive && massArtImage != null && massArtImage.sprite != null)
                SetMassArtPresentation(true);
        }

        private void ApplyPixelSprites()
        {
            if (!pixelBlockStyle) return;

            ApplySlicedSprite(background, BoardVisualUtility.GetPixelCardSprite());
            ApplySlicedSprite(border, BoardVisualUtility.GetPixelCardSprite());
            ApplySlicedSprite(depthShadow, BoardVisualUtility.GetPixelCardSprite());
            ApplySolidSprite(bottomExtrude);
            ApplySolidSprite(rightExtrude);
            ApplySlicedSprite(outerGlow, BoardVisualUtility.GetPixelCardSprite());
            ApplySlicedSprite(hoverWash, BoardVisualUtility.GetPixelCardSprite());
            ApplySolidSprite(accentStrip);
            ApplySolidSprite(headerBand);
            ApplySolidSprite(shineSweep);
            ApplySlicedSprite(cornerMarker, BoardVisualUtility.GetPixelCardSprite());
            ApplySolidSprite(titleRule);
            ApplySolidSprite(pixelGroundLip);
            ApplySolidSprite(dirtChipLeft);
            ApplySolidSprite(dirtChipRight);
            ApplySolidSprite(terrainBlockLeft);
            ApplySolidSprite(terrainBlockMid);
            ApplySolidSprite(terrainBlockRight);
            ApplyCoinSprite(coinPipTop);
            ApplyCoinSprite(coinPipMid);
            ApplyCoinSprite(coinPipLow);
            ApplySparkleSprite(sparkleTop);
            ApplySparkleSprite(sparkleMid);
            ApplySparkleSprite(sparkleLow);
        }

        private static void ApplySlicedSprite(Image img, Sprite sprite)
        {
            if (img == null || sprite == null) return;
            img.sprite = sprite;
            img.type = Image.Type.Sliced;
            img.raycastTarget = false;
        }

        private static void ApplySolidSprite(Image img)
        {
            if (img == null) return;
            img.sprite = BoardVisualUtility.GetPixelSolidSprite();
            img.type = Image.Type.Sliced;
            img.raycastTarget = false;
        }

        private static void ApplySparkleSprite(Image img)
        {
            if (img == null) return;
            img.sprite = BoardVisualUtility.GetPixelSparkleSprite();
            img.type = Image.Type.Simple;
            img.preserveAspect = true;
            img.raycastTarget = false;
        }

        private static void ApplyCoinSprite(Image img)
        {
            if (img == null) return;
            img.sprite = BoardVisualUtility.GetPixelCoinSprite();
            img.type = Image.Type.Simple;
            img.preserveAspect = true;
            img.raycastTarget = false;
        }

        private void UpdatePointerState(out bool clickedThisFrame)
        {
            clickedThisFrame = false;

            if (EventModalUI.HasVisibleModal)
            {
                _isHovered = false;
                return;
            }

            if (hitCollider == null) hitCollider = GetComponent<BoxCollider2D>();
            if (hitCollider == null || !TryGetPointerWorldPoint(out var world, out bool pointerPressed))
            {
                _isHovered = false;
                return;
            }

            bool overCard = hitCollider.OverlapPoint(world);
            bool overBlockingUi = pointerPressed ? IsPointerPressOverUi() : IsPointerOverAnyUi();
            _isHovered = overCard && !overBlockingUi;
            _hoverLocalNormalized = _isHovered ? GetNormalizedPointerInCard(world) : Vector2.zero;
            clickedThisFrame = _isHovered && pointerPressed;
        }

        private void AnimateCard()
        {
            if (!animateCard)
                return;

            CaptureMotionBaseline();

            float dt = Time.deltaTime;
            float hoverTarget = _isHovered ? 1f : 0f;
            _hoverAmount = Mathf.MoveTowards(
                _hoverAmount,
                hoverTarget,
                dt / Mathf.Max(0.01f, hoverBlendSeconds));
            _clickPunch = Mathf.MoveTowards(
                _clickPunch,
                0f,
                dt / Mathf.Max(0.01f, clickPunchSeconds));

            float time = Time.time + _motionSeed;
            float idle = Mathf.Sin(time * idleFloatSpeed) * idleFloatAmplitude;
            float lift = _hoverAmount * hoverLiftPixels *
                Mathf.Max(Mathf.Max(_motionRootBaseScale.x, _motionRootBaseScale.y), 0.001f);
            float scale = 1f + _hoverAmount * (hoverScale - 1f);
            scale += Mathf.Sin(_clickPunch * Mathf.PI) * clickPunchScale;

            if (_motionRoot != null && _motionBaselineCaptured)
            {
                _motionRoot.localScale = _motionRootBaseScale * scale;
                _motionRoot.localPosition = _motionRootBaseLocalPosition +
                    Vector3.up * (idle * Mathf.Lerp(0.45f, 1.0f, _hoverAmount) + lift);
                float tiltX = -_hoverLocalNormalized.y * maxTiltDegrees * _hoverAmount;
                float tiltY = _hoverLocalNormalized.x * maxTiltDegrees * _hoverAmount;
                float roll = Mathf.Sin(_clickPunch * Mathf.PI) * 1.6f;
                _motionRoot.localRotation = _motionRootBaseLocalRotation *
                    Quaternion.Euler(tiltX, tiltY, roll);
            }

            AnimateDecoration(time);
        }

        private void AnimateDecoration(float time)
        {
            float breathe = 0.5f + 0.5f * Mathf.Sin(time * 1.8f);
            float hoverGlow = _hoverAmount * _hoverAmount;

            if (depthShadow != null)
            {
                var rt = depthShadow.GetComponent<RectTransform>();
                rt.anchoredPosition = new Vector2(9f + _hoverAmount * 5f, -9f - _hoverAmount * 4f);
                depthShadow.color = new Color(0f, 0f, 0f, 0.22f + hoverGlow * 0.15f);
            }

            if (bottomExtrude != null)
                bottomExtrude.color = WithAlpha(Shade(_panelColor, 0.42f), 0.86f + _hoverAmount * 0.12f);
            if (rightExtrude != null)
                rightExtrude.color = WithAlpha(Shade(_panelColor, 0.50f), 0.80f + _hoverAmount * 0.14f);

            if (outerGlow != null)
                outerGlow.color = WithAlpha(Tint(_accentColor, 0.22f),
                    0.08f + hoverGlow * 0.18f + breathe * 0.035f);

            if (hoverWash != null)
                hoverWash.color = WithAlpha(Tint(_accentColor, 0.52f),
                    0.025f + _hoverAmount * 0.085f + breathe * 0.012f);

            if (accentStrip != null)
                accentStrip.color = Color.Lerp(_accentColor, Tint(_accentColor, 0.30f),
                    0.12f * breathe + 0.24f * _hoverAmount);

            if (titleRule != null)
                titleRule.color = WithAlpha(Tint(_accentColor, 0.36f), 0.78f + 0.18f * _hoverAmount);

            if (!_massArtActive)
            {
                if (pixelGroundLip != null)
                    pixelGroundLip.color = WithAlpha(PixelGrassColor(_accentColor), 0.88f + 0.08f * _hoverAmount);

                AnimateTerrainBlocks(breathe);
                AnimateCoinPips(time);
            }

            if (headerBand != null)
            {
                headerBand.color = _massArtActive
                    ? new Color(0f, 0f, 0f, 0.72f + _hoverAmount * 0.08f)
                    : WithAlpha(Shade(_panelColor, 0.24f), 0.42f + 0.10f * _hoverAmount);
            }

            if (!_massArtActive)
            {
                AnimateShine(time);
                AnimateSparkle(sparkleTop, time, 0.00f, 0.12f);
                AnimateSparkle(sparkleMid, time, 0.38f, 0.10f);
                AnimateSparkle(sparkleLow, time, 0.67f, 0.09f);
            }
        }

        private void AnimateShine(float time)
        {
            if (shineSweep == null) return;

            float t = Mathf.Repeat(time * shineCycleSpeed + _shineSeed, 1f);
            var rt = shineSweep.GetComponent<RectTransform>();
            rt.anchoredPosition = new Vector2(Mathf.Lerp(-152f, 152f, t), -3f);

            float fadeIn = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.05f, 0.20f, t));
            float fadeOut = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.74f, 0.95f, t));
            float alpha = Mathf.Min(fadeIn, fadeOut) * (0.045f + _hoverAmount * 0.07f);
            shineSweep.color = WithAlpha(Tint(_accentColor, 0.72f), alpha);
        }

        private void AnimateSparkle(Image img, float time, float phase, float baseAlpha)
        {
            if (img == null) return;

            float wave = 0.5f + 0.5f * Mathf.Sin((time * sparkleSpeed + phase) * Mathf.PI * 2f);
            float pulse = wave * wave;
            img.color = WithAlpha(Tint(_accentColor, 0.50f),
                baseAlpha + pulse * (0.10f + 0.10f * _hoverAmount));
            img.transform.localScale = Vector3.one * (0.74f + pulse * 0.38f + _hoverAmount * 0.14f);
        }

        private void AnimateTerrainBlocks(float breathe)
        {
            float emphasis = Mathf.Clamp01(_hoverAmount + breathe * 0.22f);
            ApplyTerrainBlockColors(_panelColor, _accentColor, emphasis);
            SetTerrainBlockY(terrainBlockLeft, 4f + _hoverAmount * 1.6f + breathe * 0.6f);
            SetTerrainBlockY(terrainBlockMid, 4f + _hoverAmount * 1.1f + (1f - breathe) * 0.4f);
            SetTerrainBlockY(terrainBlockRight, 4f + _hoverAmount * 1.8f + breathe * 0.5f);
        }

        private void AnimateCoinPips(float time)
        {
            AnimateCoinPip(coinPipTop, time, 0.00f);
            AnimateCoinPip(coinPipMid, time, 0.27f);
            AnimateCoinPip(coinPipLow, time, 0.54f);
            ApplyCoinPipColors(_accentColor, _hoverAmount);
        }

        private void PlayClickFeedback()
        {
            _clickPunch = 1f;
            if (isActiveAndEnabled)
                StartCoroutine(TapBurst());
        }

        private IEnumerator TapBurst()
        {
            if (_cardCanvas == null) yield break;

            var go = new GameObject("CardTapBurst", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(_cardCanvas.transform, false);

            var img = go.GetComponent<Image>();
            img.sprite = pixelBlockStyle ? BoardVisualUtility.GetPixelCardSprite() : BoardVisualUtility.GetSquareSprite();
            img.type = Image.Type.Sliced;
            img.raycastTarget = false;

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(0f, 4f);
            var burstSize = new Vector2(
                MassTextCardPrefabFactory.CardUiWidth - 8f,
                MassTextCardPrefabFactory.CardUiHeight - 8f);
            rt.sizeDelta = burstSize;

            int burstIndex = outerGlow != null ? outerGlow.transform.GetSiblingIndex() + 1 : 0;
            go.transform.SetSiblingIndex(burstIndex);

            float elapsed = 0f;
            float duration = GameConfig.AnimationDuration(0.24f);
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float eased = JuiceMath.EaseOutQuad(t);
                rt.sizeDelta = Vector2.Lerp(burstSize, burstSize * 1.18f, eased);
                img.color = WithAlpha(Tint(_accentColor, 0.38f), (1f - t) * 0.24f);
                yield return null;
            }

            Destroy(go);
        }

        private void CaptureMotionBaseline()
        {
            if (_motionRoot == null || _motionBaselineCaptured) return;
            _motionRootBaseScale = _motionRoot.localScale;
            _motionRootBaseLocalPosition = _motionRoot.localPosition;
            _motionRootBaseLocalRotation = _motionRoot.localRotation;
            _motionBaselineCaptured = true;
        }

        private void ResetMotionTransform()
        {
            if (_motionRoot == null || !_motionBaselineCaptured) return;
            _motionRoot.localScale = _motionRootBaseScale;
            _motionRoot.localPosition = _motionRootBaseLocalPosition;
            _motionRoot.localRotation = _motionRootBaseLocalRotation;
        }

        private Vector2 GetNormalizedPointerInCard(Vector3 world)
        {
            if (hitCollider == null) return Vector2.zero;

            var local = transform.InverseTransformPoint(world);
            Vector2 centered = new Vector2(local.x, local.y) - hitCollider.offset;
            Vector2 half = hitCollider.size * 0.5f;
            if (half.x <= 0.001f || half.y <= 0.001f) return Vector2.zero;

            return new Vector2(
                Mathf.Clamp(centered.x / half.x, -1f, 1f),
                Mathf.Clamp(centered.y / half.y, -1f, 1f));
        }

        private static Image CreateDecorationImage(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            img.sprite = BoardVisualUtility.GetPixelSolidSprite();
            img.type = Image.Type.Sliced;
            img.raycastTarget = false;
            return img;
        }

        private static Image CreateDotDecorationImage(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            img.sprite = BoardVisualUtility.GetPixelSparkleSprite();
            img.type = Image.Type.Simple;
            img.preserveAspect = true;
            img.raycastTarget = false;
            return img;
        }

        private static Image CreateCoinDecorationImage(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            img.sprite = BoardVisualUtility.GetPixelCoinSprite();
            img.type = Image.Type.Simple;
            img.preserveAspect = true;
            img.raycastTarget = false;
            return img;
        }

        private static TextMeshProUGUI CreateDecorationText(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontSize = 10f;
            tmp.fontStyle = FontStyles.Bold;
            tmp.enableAutoSizing = true;
            tmp.fontSizeMin = 7f;
            tmp.fontSizeMax = 10f;
            tmp.raycastTarget = false;
            TitleMenuController.ApplyJapaneseFont(tmp);
            return tmp;
        }

        private static void SetRect(Image img, Vector2 anchorMin, Vector2 anchorMax,
            Vector2 pivot, Vector2 sizeDelta, Vector2 anchoredPosition)
        {
            if (img == null) return;
            var rt = img.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = pivot;
            rt.sizeDelta = sizeDelta;
            rt.anchoredPosition = anchoredPosition;
        }

        private static void SetRect(TextMeshProUGUI tmp, Vector2 anchorMin, Vector2 anchorMax,
            Vector2 pivot, Vector2 sizeDelta, Vector2 anchoredPosition)
        {
            if (tmp == null) return;
            var rt = tmp.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = pivot;
            rt.sizeDelta = sizeDelta;
            rt.anchoredPosition = anchoredPosition;
        }

        private void ApplyTerrainBlockColors(Color panel, Color accent, float emphasis)
        {
            var grass = PixelGrassColor(accent);
            if (terrainBlockLeft != null)
                terrainBlockLeft.color = WithAlpha(Tint(grass, 0.06f + emphasis * 0.16f), 0.58f + emphasis * 0.22f);
            if (terrainBlockMid != null)
                terrainBlockMid.color = WithAlpha(Tint(grass, 0.14f + emphasis * 0.18f), 0.62f + emphasis * 0.24f);
            if (terrainBlockRight != null)
                terrainBlockRight.color = WithAlpha(Color.Lerp(Shade(panel, 0.24f), grass, 0.62f + emphasis * 0.20f),
                    0.56f + emphasis * 0.24f);
        }

        private void ApplyCoinPipColors(Color accent, float emphasis)
        {
            var coin = PixelCoinColor(accent);
            SetCoinColor(coinPipTop, coin, 0.50f + emphasis * 0.24f);
            SetCoinColor(coinPipMid, Tint(coin, 0.08f), 0.58f + emphasis * 0.26f);
            SetCoinColor(coinPipLow, Shade(coin, 0.06f), 0.48f + emphasis * 0.22f);
        }

        private void AnimateCoinPip(Image img, float time, float phase)
        {
            if (img == null) return;
            float wave = 0.5f + 0.5f * Mathf.Sin((time * (1.1f + _hoverAmount * 1.6f) + phase) * Mathf.PI * 2f);
            float spin = Mathf.Lerp(1f, Mathf.Lerp(0.48f, 1.18f, wave), _hoverAmount);
            img.transform.localScale = new Vector3(spin, 1f + _hoverAmount * 0.08f, 1f);
        }

        private static void SetCoinColor(Image img, Color color, float alpha)
        {
            if (img == null) return;
            img.color = WithAlpha(color, alpha);
        }

        private static void SetTerrainBlockY(Image img, float y)
        {
            if (img == null) return;
            var rt = img.GetComponent<RectTransform>();
            var pos = rt.anchoredPosition;
            pos.y = y;
            rt.anchoredPosition = pos;
        }

        private static Color Tint(Color c, float amount) => Color.Lerp(c, Color.white, amount);
        private static Color Shade(Color c, float amount) => Color.Lerp(c, Color.black, amount);
        private static Color WithAlpha(Color c, float a) => new(c.r, c.g, c.b, a);
        private static Color PixelPanelColor(Color c)
        {
            var brick = new Color(0.70f, 0.40f, 0.19f, 0.98f);
            var tintedBrick = Color.Lerp(brick, Tint(c, 0.18f), 0.42f);
            tintedBrick.a = 0.98f;
            return tintedBrick;
        }

        private static Color PixelAccentColor(Color c)
        {
            var grass = new Color(0.24f, 0.78f, 0.22f, 1f);
            var warmHighlight = new Color(1f, 0.78f, 0.22f, 1f);
            var typed = Tint(c, 0.10f);
            var accent = Color.Lerp(grass, typed, 0.48f);
            accent = Color.Lerp(accent, warmHighlight, 0.12f);
            accent.a = 1f;
            return accent;
        }

        private static Color PixelGrassColor(Color c)
        {
            var baseGrass = new Color(0.22f, 0.78f, 0.18f, 1f);
            var typeTint = Tint(c, 0.24f);
            var grass = Color.Lerp(baseGrass, typeTint, 0.34f);
            grass.a = 1f;
            return grass;
        }

        private static Color PixelCoinColor(Color c)
        {
            var coin = new Color(1f, 0.74f, 0.20f, 1f);
            var typed = Tint(c, 0.22f);
            var mixed = Color.Lerp(coin, typed, 0.22f);
            mixed.a = 1f;
            return mixed;
        }

        private void EnsureCanvasRenders()
        {
            var canvas = GetComponentInChildren<Canvas>(true);
            if (canvas == null) return;

            if (canvas.worldCamera == null)
                canvas.worldCamera = Camera.main;

            canvas.overrideSorting = true;
            if (SortingLayerExists(BoardSortingLayers.Board))
                canvas.sortingLayerName = BoardSortingLayers.Board;
            if (canvas.sortingOrder < BoardSortingLayers.WaypointBaseOrder)
                canvas.sortingOrder = BoardSortingLayers.WaypointBaseOrder + 80;
        }

        private static bool SortingLayerExists(string layerName)
        {
            foreach (var layer in SortingLayer.layers)
                if (layer.name == layerName) return true;
            return false;
        }

        private static bool TryGetPointerWorldPoint(out Vector3 world, out bool pressedThisFrame)
        {
            world = Vector3.zero;
            pressedThisFrame = false;
            var cam = Camera.main;
            if (cam == null) return false;

            Vector2? screen = null;
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                screen = Mouse.current.position.ReadValue();
                pressedThisFrame = true;
            }
            else if (Mouse.current != null)
            {
                screen = Mouse.current.position.ReadValue();
            }
            else if (Touchscreen.current != null)
            {
                var t = Touchscreen.current.primaryTouch;
                if (t.press.wasPressedThisFrame || t.press.isPressed)
                {
                    screen = t.position.ReadValue();
                    pressedThisFrame = t.press.wasPressedThisFrame;
                }
            }

            if (screen == null) return false;
            var p = screen.Value;
            world = cam.ScreenToWorldPoint(new Vector3(p.x, p.y, -cam.transform.position.z));
            return true;
        }

        private static bool IsPointerPressOverUi()
        {
            var eventSystem = EventSystem.current;
            if (eventSystem == null) return false;

            if (Mouse.current != null &&
                Mouse.current.leftButton.wasPressedThisFrame &&
                eventSystem.IsPointerOverGameObject())
                return true;

            if (Touchscreen.current != null)
            {
                var touch = Touchscreen.current.primaryTouch;
                if (touch.press.wasPressedThisFrame &&
                    eventSystem.IsPointerOverGameObject(touch.touchId.ReadValue()))
                    return true;
            }

            return false;
        }

        private static bool IsPointerOverAnyUi()
        {
            var eventSystem = EventSystem.current;
            if (eventSystem == null) return false;

            if (Mouse.current != null && eventSystem.IsPointerOverGameObject())
                return true;

            if (Touchscreen.current != null)
            {
                var touch = Touchscreen.current.primaryTouch;
                if (touch.press.isPressed &&
                    eventSystem.IsPointerOverGameObject(touch.touchId.ReadValue()))
                    return true;
            }

            return false;
        }
    }
}
