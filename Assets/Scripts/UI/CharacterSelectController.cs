using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using TMPro;
using Sugoroku.Data;
using Sugoroku.Game;
using Sugoroku.Visual;
using Sugoroku.Board;

namespace Sugoroku.UI
{
    public class CharacterSelectController : MonoBehaviour
    {
        private static readonly CharacterType[] CharacterTypes =
            (CharacterType[])System.Enum.GetValues(typeof(CharacterType));

        [SerializeField] private TextMeshProUGUI _classNameText;
        [SerializeField] private TextMeshProUGUI _traitNameText;
        [SerializeField] private TextMeshProUGUI _traitDescText;
        [SerializeField] private TextMeshProUGUI _roleText;
        [SerializeField] private TextMeshProUGUI _statsText;
        [SerializeField] private RectTransform     _cardParent;
        [SerializeField] private Button          _prevButton;
        [SerializeField] private Button          _nextButton;
        [SerializeField] private Button          _confirmButton;
        [SerializeField] private Button          _backButton;
        [SerializeField] private Image           _portraitImage;
        [SerializeField] private TextMeshProUGUI _screenTitle;

        [Header("動的カード（シーン配置の CardParent を利用）")]
        [SerializeField] private Vector2 _cardPreferredSize = new(140f, 200f);

        private CharacterType _selected;
        private int           _humanSlot;
        private int           _index;
        private int           _lastJuiceIndex = -1;
        private Button[]      _cardButtons;
        private CharacterSelectJuice _juice;
        private bool _confirming;
        private RectTransform _profileMeterGroup;
        private RectTransform _roleBadgeGroup;
        private RectTransform _playerSlotBadge;
        private RectTransform _pagerPips;
        private RectTransform _focusPlaque;

        private void Awake()
        {
            JapaneseFontProvider.WarmUp();
            var canvas = GetComponentInParent<Canvas>();
            if (canvas != null)
                JapaneseFontProvider.ApplyAllInCanvas(canvas);
        }

        private void Start()
        {
            BindReferences();
            DisableRootRaycastBlocker();
            _juice = GetComponent<CharacterSelectJuice>();
            if (_juice == null) _juice = gameObject.AddComponent<CharacterSelectJuice>();
            EnsureEventSystem();
            GameSession.EnsureHumanCharacters();
            _humanSlot = 0;
            _selected  = GameSession.GetHumanCharacter(0);
            _index     = IndexOfCharacter(_selected);
            ApplySceneLayout();
            BuildCardButtons();
            WireButtons();
            var canvas = GetComponentInParent<Canvas>();
            if (canvas != null) KenneyUiStyler.StyleCanvas(canvas);
            ApplyScreenChrome();
            RefreshPanel(playJuice: false);
        }

        private void DisableRootRaycastBlocker()
        {
            var bg = GetComponent<Image>();
            if (bg != null)
            {
                bg.raycastTarget = false;
                bg.sprite = BoardVisualUtility.GetPixelPlatformBackdropSprite();
                bg.type = Image.Type.Simple;
                bg.preserveAspect = false;
                bg.color = new Color(0.86f, 0.94f, 1f, 0.96f);
            }
        }

        private static void EnsureEventSystem()
        {
            if (FindFirstObjectByType<EventSystem>() != null) return;
            var es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<InputSystemUIInputModule>();
        }

        private void BindReferences()
        {
            _classNameText ??= FindInChildren<TextMeshProUGUI>("ClassNameText");
            _traitNameText ??= FindInChildren<TextMeshProUGUI>("TraitNameText");
            _traitDescText ??= FindInChildren<TextMeshProUGUI>("TraitDescText");
            _roleText      ??= FindInChildren<TextMeshProUGUI>("RoleText");
            _statsText     ??= FindInChildren<TextMeshProUGUI>("StatsText");
            _cardParent    ??= FindRectTransform("CardParent");
            _prevButton    ??= FindInChildren<Button>("PrevButton");
            _nextButton    ??= FindInChildren<Button>("NextButton");
            _confirmButton ??= FindInChildren<Button>("ConfirmButton");
            _backButton    ??= FindInChildren<Button>("BackButton");
            _portraitImage ??= FindInChildren<Image>("PortraitImage");
            _screenTitle   ??= FindInChildren<TextMeshProUGUI>("ScreenTitle");
            _profileMeterGroup ??= FindRectTransform("ProfileMeterGroup");
            _roleBadgeGroup    ??= FindRectTransform("CharacterRoleBadgeGroup");
            _playerSlotBadge   ??= FindRectTransform("PlayerSlotBadge");
            _pagerPips         ??= FindRectTransform("CharacterPagerPips");
            _focusPlaque       ??= FindRectTransform("CharacterFocusPlaque");
        }

        private T FindInChildren<T>(string objectName) where T : Component
        {
            var t = transform.Find(objectName);
            if (t != null)
            {
                var c = t.GetComponent<T>();
                if (c != null) return c;
            }

            foreach (Transform child in GetComponentsInChildren<Transform>(true))
            {
                if (!string.Equals(child.name, objectName, System.StringComparison.Ordinal)) continue;
                var c = child.GetComponent<T>();
                if (c != null) return c;
            }

            var canvas = GetComponentInParent<Canvas>();
            if (canvas != null)
            {
                foreach (Transform child in canvas.GetComponentsInChildren<Transform>(true))
                {
                    if (!string.Equals(child.name, objectName, System.StringComparison.Ordinal)) continue;
                    var c = child.GetComponent<T>();
                    if (c != null) return c;
                }
            }

            var global = GameObject.Find(objectName);
            return global != null ? global.GetComponent<T>() : null;
        }

        private RectTransform FindRectTransform(string objectName)
        {
            var direct = transform.Find(objectName) as RectTransform;
            if (direct != null) return direct;

            foreach (Transform child in GetComponentsInChildren<Transform>(true))
            {
                if (!string.Equals(child.name, objectName, System.StringComparison.Ordinal)) continue;
                if (child is RectTransform rt) return rt;
            }

            var canvas = GetComponentInParent<Canvas>();
            if (canvas != null)
            {
                foreach (Transform child in canvas.GetComponentsInChildren<Transform>(true))
                {
                    if (!string.Equals(child.name, objectName, System.StringComparison.Ordinal)) continue;
                    if (child is RectTransform rt) return rt;
                }
            }

            var global = GameObject.Find(objectName);
            return global != null ? global.transform as RectTransform : null;
        }

        private RectTransform EnsureCardParent()
        {
            var existing = FindRectTransform("CardParent");
            if (existing != null) return existing;

            var go = new GameObject("CardParent", typeof(RectTransform));
            go.transform.SetParent(transform, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(-280f, 80f);
            rt.sizeDelta = new Vector2(900f, 140f);
            var h = go.AddComponent<HorizontalLayoutGroup>();
            h.spacing = 12;
            h.childAlignment = TextAnchor.MiddleCenter;
            Debug.LogWarning("[CharacterSelect] CardParent を自動作成しました。");
            return rt;
        }

        private void ApplySceneLayout()
        {
            _cardParent ??= EnsureCardParent();
            _cardPreferredSize = new Vector2(142f, 226f);

            PlaceRect(_cardParent, new Vector2(-405f, 62f), new Vector2(830f, 300f));
            if (_cardParent != null)
            {
                var h = _cardParent.GetComponent<HorizontalLayoutGroup>() ?? _cardParent.gameObject.AddComponent<HorizontalLayoutGroup>();
                h.spacing = 15f;
                h.childAlignment = TextAnchor.MiddleCenter;
                h.childControlWidth = true;
                h.childControlHeight = true;
                h.childForceExpandWidth = false;
                h.childForceExpandHeight = false;
            }

            PlaceButton(_backButton, new Vector2(-790f, -466f), new Vector2(180f, 58f), HudTextStyle.Scale(15f));
            PlaceButton(_prevButton, new Vector2(-560f, -466f), new Vector2(180f, 58f), HudTextStyle.Scale(18f));
            PlaceRect(_pagerPips, new Vector2(-310f, -466f), new Vector2(260f, 36f));
            PlaceButton(_nextButton, new Vector2(-60f, -466f), new Vector2(180f, 58f), HudTextStyle.Scale(18f));
            PlaceButton(_confirmButton, new Vector2(530f, -466f), new Vector2(340f, 58f), HudTextStyle.Scale(16f));

            PlaceText(_screenTitle, new Vector2(0f, 480f), new Vector2(760f, 52f), HudTextStyle.Scale(24f),
                TextAlignmentOptions.Center);
            PlaceRect(_playerSlotBadge, new Vector2(-790f, 480f), new Vector2(154f, 46f));

            PlaceRect(_focusPlaque, new Vector2(430f, 352f), new Vector2(390f, 44f));
            PlaceImage(_portraitImage, new Vector2(430f, 250f), new Vector2(168f, 168f));
            PlaceText(_classNameText, new Vector2(430f, 146f), new Vector2(470f, 46f), HudTextStyle.Scale(24f),
                TextAlignmentOptions.Center);
            PlaceText(_traitNameText, new Vector2(430f, 100f), new Vector2(470f, 40f), HudTextStyle.Scale(16f),
                TextAlignmentOptions.Center);
            PlaceText(_traitDescText, new Vector2(430f, 30f), new Vector2(470f, 88f), HudTextStyle.Scale(14f),
                TextAlignmentOptions.TopLeft, wrap: true);
            PlaceRect(_roleBadgeGroup, new Vector2(430f, -40f), new Vector2(470f, 34f));
            PlaceText(_roleText, new Vector2(430f, -100f), new Vector2(470f, 76f), HudTextStyle.Scale(13f),
                TextAlignmentOptions.TopLeft, wrap: true);
            PlaceText(_statsText, new Vector2(430f, -164f), new Vector2(470f, 40f), HudTextStyle.Scale(14f),
                TextAlignmentOptions.Center);
            PlaceRect(_profileMeterGroup, new Vector2(430f, -246f), new Vector2(470f, 116f));
        }

        private static void PlaceRect(RectTransform rt, Vector2 pos, Vector2 size)
        {
            if (rt == null) return;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
        }

        private static void PlaceImage(Image image, Vector2 pos, Vector2 size)
        {
            if (image == null) return;
            PlaceRect(image.rectTransform, pos, size);
        }

        private static void PlaceText(TextMeshProUGUI tmp, Vector2 pos, Vector2 size, float fontSize,
            TextAlignmentOptions alignment, bool wrap = false)
        {
            if (tmp == null) return;
            PlaceRect(tmp.rectTransform, pos, size);
            tmp.fontSize = fontSize;
            tmp.alignment = alignment;
            tmp.textWrappingMode = wrap ? TextWrappingModes.Normal : TextWrappingModes.NoWrap;
            tmp.overflowMode = TextOverflowModes.Ellipsis;
            JapaneseFontProvider.Apply(tmp);
        }

        private static void PlaceButton(Button button, Vector2 pos, Vector2 size, float labelSize)
        {
            if (button == null) return;
            var rt = button.GetComponent<RectTransform>();
            PlaceRect(rt, pos, size);
            var le = button.GetComponent<LayoutElement>();
            if (le != null)
            {
                le.ignoreLayout = true;
                le.preferredWidth = size.x;
                le.preferredHeight = size.y;
            }

            foreach (var tmp in button.GetComponentsInChildren<TextMeshProUGUI>(true))
            {
                tmp.fontSize = labelSize;
                tmp.enableAutoSizing = true;
                tmp.fontSizeMin = Mathf.Min(labelSize, 13f);
                tmp.fontSizeMax = labelSize;
                tmp.textWrappingMode = TextWrappingModes.NoWrap;
                tmp.overflowMode = TextOverflowModes.Ellipsis;
            }
        }

        private void EnsureCharacterSelectDepthBackdrop(Color accent)
        {
            var backdrop = EnsureScreenImage("CharacterSelectDepthBackdrop");
            StretchRect(backdrop.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            backdrop.sprite = BoardVisualUtility.GetPixelPlatformBackdropSprite();
            backdrop.type = Image.Type.Simple;
            backdrop.preserveAspect = false;
            backdrop.color = new Color(0.86f, 0.94f, 1f, 0.92f);
            SetSiblingSafe(backdrop.rectTransform, 0);

            var clouds = EnsureScreenImage("CharacterSelectCloudParallax");
            StretchRect(clouds.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            clouds.sprite = BoardVisualUtility.GetPixelCloudDepthSprite();
            clouds.type = Image.Type.Simple;
            clouds.preserveAspect = false;
            clouds.color = new Color(1f, 1f, 1f, 0.34f);
            SetSiblingSafe(clouds.rectTransform, 1);

            var floor = EnsureScreenImage("CharacterSelectFloorShadow");
            SetRect(floor.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0.5f), new Vector2(1040f, 150f), new Vector2(-70f, 86f));
            floor.sprite = BoardVisualUtility.GetSoftOvalShadowSprite();
            floor.type = Image.Type.Simple;
            floor.color = new Color(0f, 0f, 0f, 0.20f);
            SetSiblingSafe(floor.rectTransform, 2);

            var foreground = EnsureScreenImage("CharacterSelectForegroundParallax");
            StretchRect(foreground.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            foreground.sprite = BoardVisualUtility.GetPixelForegroundDepthSprite();
            foreground.type = Image.Type.Simple;
            foreground.preserveAspect = false;
            foreground.color = new Color(1f, 1f, 1f, 0.30f);
            SetSiblingSafe(foreground.rectTransform, 3);

            var accentPlate = EnsureScreenImage("CharacterSelectAccentPlate");
            SetRect(accentPlate.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f), new Vector2(820f, 10f), new Vector2(0f, -24f));
            accentPlate.sprite = BoardVisualUtility.GetPixelSolidSprite();
            accentPlate.type = Image.Type.Sliced;
            accentPlate.color = new Color(accent.r, accent.g, accent.b, 0.78f);
            SetSiblingSafe(accentPlate.rectTransform, 4);
        }

        private Image EnsureScreenImage(string name)
        {
            var child = transform.Find(name);
            Image img;
            if (child == null)
            {
                var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(LayoutElement));
                go.transform.SetParent(transform, false);
                go.GetComponent<LayoutElement>().ignoreLayout = true;
                img = go.GetComponent<Image>();
            }
            else
            {
                img = child.GetComponent<Image>() ?? child.gameObject.AddComponent<Image>();
            }

            img.raycastTarget = false;
            return img;
        }

        private static Image EnsureCardImage(Transform parent, string name, Sprite sprite, Color color, Image.Type type)
        {
            var child = parent.Find(name);
            Image img;
            if (child == null)
            {
                var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(LayoutElement));
                go.transform.SetParent(parent, false);
                go.GetComponent<LayoutElement>().ignoreLayout = true;
                img = go.GetComponent<Image>();
            }
            else
            {
                img = child.GetComponent<Image>() ?? child.gameObject.AddComponent<Image>();
                var le = child.GetComponent<LayoutElement>() ?? child.gameObject.AddComponent<LayoutElement>();
                le.ignoreLayout = true;
            }

            img.sprite = sprite != null ? sprite : BoardVisualUtility.GetPixelSolidSprite();
            img.type = type;
            img.color = color;
            img.raycastTarget = false;
            return img;
        }

        private static TextMeshProUGUI EnsureCardLabel(Transform parent, string name)
        {
            var child = parent.Find(name);
            TextMeshProUGUI tmp;
            if (child == null)
            {
                var go = new GameObject(name, typeof(RectTransform), typeof(LayoutElement));
                go.transform.SetParent(parent, false);
                go.GetComponent<LayoutElement>().ignoreLayout = true;
                tmp = go.AddComponent<TextMeshProUGUI>();
            }
            else
            {
                tmp = child.GetComponent<TextMeshProUGUI>() ?? child.gameObject.AddComponent<TextMeshProUGUI>();
                var le = child.GetComponent<LayoutElement>() ?? child.gameObject.AddComponent<LayoutElement>();
                le.ignoreLayout = true;
            }

            JapaneseFontProvider.Apply(tmp);
            tmp.enableAutoSizing = true;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            tmp.overflowMode = TextOverflowModes.Ellipsis;
            tmp.fontStyle = FontStyles.Bold;
            tmp.raycastTarget = false;
            tmp.transform.SetAsLastSibling();
            return tmp;
        }

        private static void SetSiblingSafe(RectTransform rt, int index)
        {
            if (rt == null || rt.parent == null) return;
            rt.SetSiblingIndex(Mathf.Clamp(index, 0, rt.parent.childCount - 1));
        }

        private static void StretchRect(RectTransform rt, Vector2 anchorMin, Vector2 anchorMax,
            Vector2 offsetMin, Vector2 offsetMax)
        {
            if (rt == null) return;
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = offsetMin;
            rt.offsetMax = offsetMax;
        }

        private static void SetRect(RectTransform rt, Vector2 anchorMin, Vector2 anchorMax,
            Vector2 pivot, Vector2 sizeDelta, Vector2 anchoredPosition)
        {
            if (rt == null) return;
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = pivot;
            rt.sizeDelta = sizeDelta;
            rt.anchoredPosition = anchoredPosition;
        }

        private void BuildCardButtons()
        {
            _cardParent ??= EnsureCardParent();
            if (_cardParent == null)
            {
                Debug.LogWarning("[CharacterSelect] CardParent が見つかりません。カード選択 UI を構築できません。");
                return;
            }

            foreach (Transform c in _cardParent)
                Destroy(c.gameObject);

            var layout = _cardParent.GetComponent<HorizontalLayoutGroup>();

            var types = CharacterTypes;
            _cardButtons = new Button[types.Length];
            for (int i = 0; i < types.Length; i++)
            {
                int idx = i;
                var type = types[i];
                var go = new GameObject(type.DisplayName(), typeof(RectTransform));
                go.transform.SetParent(_cardParent, false);

                var rt = go.GetComponent<RectTransform>();
                if (layout != null)
                {
                    var le = go.AddComponent<LayoutElement>();
                    le.preferredWidth  = _cardPreferredSize.x;
                    le.preferredHeight = _cardPreferredSize.y;
                }
                else
                    rt.sizeDelta = _cardPreferredSize;

                var hitArea = go.AddComponent<Image>();
                hitArea.sprite = BoardVisualUtility.GetPixelSolidSprite();
                hitArea.type = Image.Type.Sliced;
                hitArea.color = new Color(1f, 1f, 1f, 0.01f);
                hitArea.raycastTarget = true;

                var halo = EnsureCardImage(go.transform, "SelectedCardHalo", BoardVisualUtility.GetSoftOvalShadowSprite(),
                    new Color(type.AccentColor().r, type.AccentColor().g, type.AccentColor().b, 0f), Image.Type.Simple);
                halo.preserveAspect = false;
                SetRect(halo.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                    new Vector2(0.5f, 0.5f), new Vector2(170f, 260f), new Vector2(0f, -8f));

                var shadow = EnsureCardImage(go.transform, "CardDepthShadow", BoardVisualUtility.GetPixelCardSprite(),
                    new Color(0f, 0f, 0f, 0.28f), Image.Type.Sliced);
                StretchRect(shadow.rectTransform, Vector2.zero, Vector2.one,
                    new Vector2(10f, -14f), new Vector2(10f, -14f));

                var rightExtrude = EnsureCardImage(go.transform, "CardRightExtrude", BoardVisualUtility.GetPixelSolidSprite(),
                    Color.Lerp(Color.black, type.AccentColor(), 0.20f), Image.Type.Sliced);
                SetRect(rightExtrude.rectTransform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
                    new Vector2(1f, 0.5f), new Vector2(10f, 176f), new Vector2(8f, -7f));

                var bottomExtrude = EnsureCardImage(go.transform, "CardBottomExtrude", BoardVisualUtility.GetPixelSolidSprite(),
                    Color.Lerp(Color.black, type.AccentColor(), 0.18f), Image.Type.Sliced);
                SetRect(bottomExtrude.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                    new Vector2(0.5f, 0f), new Vector2(112f, 10f), new Vector2(6f, -9f));

                var face = EnsureCardImage(go.transform, "CardFace", BoardVisualUtility.GetPixelCardSprite(),
                    new Color(0.15f, 0.19f, 0.27f, 0.98f), Image.Type.Sliced);
                StretchRect(face.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

                var topRule = EnsureCardImage(go.transform, "CardTopRule", BoardVisualUtility.GetPixelSolidSprite(),
                    type.AccentColor(), Image.Type.Sliced);
                SetRect(topRule.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                    new Vector2(0.5f, 1f), new Vector2(104f, 9f), new Vector2(0f, -13f));

                var numberBadge = EnsureCardImage(go.transform, "CharacterNumberBadge", BoardVisualUtility.GetPixelCardSprite(),
                    new Color(type.AccentColor().r * 0.42f, type.AccentColor().g * 0.42f, type.AccentColor().b * 0.42f, 0.88f),
                    Image.Type.Sliced);
                SetRect(numberBadge.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f),
                    new Vector2(0f, 1f), new Vector2(36f, 22f), new Vector2(9f, -9f));
                var numberLabel = EnsureCardLabel(numberBadge.transform, "Label");
                numberLabel.text = $"{i + 1:00}";
                numberLabel.fontSize = HudTextStyle.Scale(9f);
                numberLabel.fontSizeMin = 7f;
                numberLabel.fontSizeMax = HudTextStyle.Scale(9f);
                StretchRect(numberLabel.rectTransform, Vector2.zero, Vector2.one, new Vector2(3f, 0f), new Vector2(-3f, 0f));

                var selectedTab = EnsureCardImage(go.transform, "SelectedTab", BoardVisualUtility.GetPixelCardSprite(),
                    new Color(type.AccentColor().r, type.AccentColor().g, type.AccentColor().b, 0f), Image.Type.Sliced);
                SetRect(selectedTab.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                    new Vector2(0.5f, 0.5f), new Vector2(82f, 24f), new Vector2(0f, 3f));
                var selectedTabLabel = EnsureCardLabel(selectedTab.transform, "Label");
                selectedTabLabel.text = "選択中";
                selectedTabLabel.fontSize = HudTextStyle.Scale(9f);
                selectedTabLabel.fontSizeMin = 7f;
                selectedTabLabel.fontSizeMax = HudTextStyle.Scale(9f);
                StretchRect(selectedTabLabel.rectTransform, Vector2.zero, Vector2.one, new Vector2(4f, 0f), new Vector2(-4f, 0f));

                var portraitWindow = EnsureCardImage(go.transform, "PortraitWindow", BoardVisualUtility.GetPixelCardSprite(),
                    new Color(0.08f, 0.11f, 0.17f, 0.94f), Image.Type.Sliced);
                SetRect(portraitWindow.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                    new Vector2(0.5f, 0.5f), new Vector2(110f, 112f), new Vector2(0f, 35f));

                var portraitImage = EnsureCardImage(go.transform, "Portrait", OriginalcharAssets.GetSprite(type),
                    Color.white, Image.Type.Simple);
                portraitImage.preserveAspect = true;
                SetRect(portraitImage.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                    new Vector2(0.5f, 0.5f), new Vector2(98f, 98f), new Vector2(0f, 39f));

                var roleRibbon = EnsureCardImage(go.transform, "RoleRibbon", BoardVisualUtility.GetPixelCardSprite(),
                    new Color(type.AccentColor().r * 0.36f, type.AccentColor().g * 0.36f, type.AccentColor().b * 0.36f, 0.84f),
                    Image.Type.Sliced);
                SetRect(roleRibbon.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                    new Vector2(0.5f, 0.5f), new Vector2(96f, 19f), new Vector2(0f, 80f));
                var roleRibbonLabel = EnsureCardLabel(roleRibbon.transform, "Label");
                roleRibbonLabel.text = RoleTag(type);
                roleRibbonLabel.fontSize = HudTextStyle.Scale(8f);
                roleRibbonLabel.fontSizeMin = 6f;
                roleRibbonLabel.fontSizeMax = HudTextStyle.Scale(8f);
                StretchRect(roleRibbonLabel.rectTransform, Vector2.zero, Vector2.one, new Vector2(5f, 0f), new Vector2(-5f, 0f));

                var chip = EnsureCardImage(go.transform, "TraitChip", BoardVisualUtility.GetPixelCardSprite(),
                    new Color(type.AccentColor().r * 0.34f, type.AccentColor().g * 0.34f, type.AccentColor().b * 0.34f, 0.92f),
                    Image.Type.Sliced);
                SetRect(chip.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                    new Vector2(0.5f, 0.5f), new Vector2(108f, 24f), new Vector2(0f, -45f));
                var traitText = EnsureCardLabel(chip.transform, "TraitText");
                traitText.text = type.TraitName();
                traitText.fontSize = HudTextStyle.Scale(10f);
                traitText.fontSizeMin = 8f;
                traitText.fontSizeMax = HudTextStyle.Scale(10f);
                StretchRect(traitText.rectTransform, Vector2.zero, Vector2.one, new Vector2(6f, 0f), new Vector2(-6f, 0f));

                var meter = EnsureCardImage(go.transform, "SelectionMeter", BoardVisualUtility.GetPixelSolidSprite(),
                    new Color(type.AccentColor().r, type.AccentColor().g, type.AccentColor().b, 0.38f), Image.Type.Sliced);
                SetRect(meter.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                    new Vector2(0.5f, 0.5f), new Vector2(108f, 7f), new Vector2(0f, -66f));

                var sparkleA = EnsureCardImage(go.transform, "CardSparkleA", BoardVisualUtility.GetPixelSparkleSprite(),
                    new Color(1f, 1f, 1f, 0.36f), Image.Type.Simple);
                SetRect(sparkleA.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                    new Vector2(0.5f, 0.5f), new Vector2(14f, 14f), new Vector2(50f, 83f));
                var sparkleB = EnsureCardImage(go.transform, "CardSparkleB", BoardVisualUtility.GetPixelSparkleSprite(),
                    new Color(type.AccentColor().r, type.AccentColor().g, type.AccentColor().b, 0.32f), Image.Type.Simple);
                SetRect(sparkleB.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                    new Vector2(0.5f, 0.5f), new Vector2(11f, 11f), new Vector2(-51f, 74f));

                var tmp = EnsureCardLabel(go.transform, "Label");
                tmp.text = type.DisplayName();
                tmp.fontSize = HudTextStyle.Scale(13f);
                tmp.fontSizeMin = 11f;
                tmp.fontSizeMax = HudTextStyle.Scale(13f);
                SetRect(tmp.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                    new Vector2(0.5f, 0f), new Vector2(124f, 38f), new Vector2(0f, 8f));

                var btn = go.AddComponent<Button>();
                btn.targetGraphic = face;
                var colors = btn.colors;
                colors.normalColor = Color.white;
                colors.highlightedColor = new Color(1.08f, 1.08f, 1.04f, 1f);
                colors.pressedColor = new Color(0.84f, 0.86f, 0.92f, 1f);
                colors.selectedColor = colors.highlightedColor;
                colors.disabledColor = new Color(0.48f, 0.50f, 0.54f, 0.58f);
                colors.fadeDuration = 0.06f;
                btn.colors = colors;

                StyleCharacterCardShell(go.transform, type.AccentColor(), selected: false);

                btn.onClick.AddListener(() => SelectIndex(idx));
                var hover = go.AddComponent<CharacterSelectCardHover>();
                hover.Setup(this, idx);
                _cardButtons[i] = btn;
            }
        }

        public void OnCardHover(int index)
        {
            if (_confirming || index == _index) return;
            SelectIndex(index, fromHover: true);
        }

        private void SelectIndex(int index, bool fromHover = false)
        {
            _index    = NormalizeCharacterIndex(index);
            _selected = CharacterAt(_index);
            RefreshPanel(playJuice: true, isNewSelection: !fromHover || _lastJuiceIndex != _index);
        }

        private void WireButtons()
        {
            if (_prevButton != null)
            {
                _prevButton.onClick.RemoveAllListeners();
                _prevButton.onClick.AddListener(() => SelectIndex(_index - 1));
            }
            if (_nextButton != null)
            {
                _nextButton.onClick.RemoveAllListeners();
                _nextButton.onClick.AddListener(() => SelectIndex(_index + 1));
            }
            if (_confirmButton != null)
            {
                _confirmButton.onClick.RemoveAllListeners();
                _confirmButton.onClick.AddListener(OnConfirm);
            }
            if (_backButton != null)
            {
                _backButton.onClick.RemoveAllListeners();
                _backButton.onClick.AddListener(() => SceneManager.LoadScene("TitleScene"));
            }
        }

        private void RefreshPanel(bool playJuice = true, bool isNewSelection = true)
        {
            var slotColor = PlayerIdentity.SlotColors[_humanSlot % PlayerIdentity.SlotColors.Length];
            if (_screenTitle != null)
            {
                if (GameSession.HumanCount > 1)
                    _screenTitle.text = $"プレイヤー {_humanSlot + 1} / {GameSession.HumanCount} のキャラ選択";
                else
                    _screenTitle.text = "キャラ選択";
                _screenTitle.color = slotColor;
            }
            UpdatePlayerSlotBadge(slotColor);
            ApplyPixelActionButtons(_selected.AccentColor());

            if (_confirmButton != null)
            {
                var label = _confirmButton.GetComponentInChildren<TextMeshProUGUI>();
                if (label != null)
                {
                    bool last = _humanSlot >= GameSession.HumanCount - 1;
                    label.text = last ? "このキャラで開始" : "次のプレイヤーへ";
                }
            }

            var p = _selected.GetProfile();
            if (_classNameText != null)
                _classNameText.text = _selected.DisplayName();
            if (_traitNameText != null)
                _traitNameText.text = $"固有特性: {_selected.TraitName()}";
            if (_traitDescText != null)
                _traitDescText.text = _selected.TraitDescription();
            if (_roleText != null)
                _roleText.text = $"戦略的役割\n{_selected.StrategicRole()}";
            if (_statsText != null)
                _statsText.text = "初期ステータス / プレイ傾向";

            UpdateRoleBadges(_selected);
            UpdateProfileMeters(_selected, p);
            UpdateCharacterPagerPips();
            UpdateFocusPlaque(_selected);

            if (_portraitImage != null)
            {
                var portrait = OriginalcharAssets.GetSprite(_selected);
                _portraitImage.sprite = portrait;
                _portraitImage.preserveAspect = true;
                _portraitImage.color = portrait != null ? Color.white : new Color(0.2f, 0.22f, 0.35f);
                var frame = transform.Find("PortraitFrame");
                if (frame != null)
                    GameUiChrome.ApplyAccentRail(frame, _selected.AccentColor(), 5f);
            }

            UpdateScreenAccent(_selected.AccentColor());

            if (_cardButtons == null) return;

            var types = CharacterTypes;
            for (int i = 0; i < _cardButtons.Length; i++)
            {
                if (_cardButtons[i] == null) continue;
                var cardTransform = _cardButtons[i].transform;
                bool sel = i == _index;
                var spr = OriginalcharAssets.GetSprite(types[i]);
                var portrait = cardTransform.Find("Portrait")?.GetComponent<Image>();
                if (portrait != null)
                {
                    portrait.sprite = spr;
                    portrait.preserveAspect = true;
                    portrait.color = spr != null
                        ? (sel ? Color.white : new Color(0.76f, 0.80f, 0.86f, 1f))
                        : new Color(types[i].AccentColor().r, types[i].AccentColor().g, types[i].AccentColor().b, sel ? 0.92f : 0.56f);
                }

                var trait = cardTransform.Find("TraitChip/TraitText")?.GetComponent<TextMeshProUGUI>();
                if (trait != null)
                    trait.text = types[i].TraitName();

                var roleRibbonLabel = cardTransform.Find("RoleRibbon/Label")?.GetComponent<TextMeshProUGUI>();
                if (roleRibbonLabel != null)
                    roleRibbonLabel.text = RoleTag(types[i]);

                StyleCharacterCardShell(cardTransform, types[i].AccentColor(), sel);
                cardTransform.localScale = sel ? Vector3.one * 1.10f : Vector3.one * 0.98f;
                cardTransform.localRotation = sel
                    ? Quaternion.Euler(-2.2f, -7.2f, 1.1f)
                    : Quaternion.Euler(0f, 0f, i % 2 == 0 ? 0.45f : -0.45f);

                if (sel && playJuice && isNewSelection && _lastJuiceIndex != i)
                {
                    _juice?.PlaySelectionChanged(cardTransform, isNewSelection);
                    _lastJuiceIndex = i;
                }
            }
        }

        private void ApplyScreenChrome()
        {
            ApplySceneLayout();
            EnsureCharacterSelectDepthBackdrop(_selected.AccentColor());

            var deckPanel = EnsureBackdrop("CardDeckPanel", new Vector2(-405f, 44f), new Vector2(890f, 300f),
                new Color(0.11f, 0.15f, 0.22f, 0.90f), new Color(0.50f, 0.66f, 0.88f, 0.76f));
            var infoPanel = EnsureBackdrop("CharacterInfoPanel", new Vector2(430f, 20f), new Vector2(560f, 760f),
                new Color(0.10f, 0.14f, 0.20f, 0.94f), new Color(0.86f, 0.68f, 0.28f, 0.92f));
            SetSiblingSafe(deckPanel, 5);
            SetSiblingSafe(infoPanel, 6);

            if (_portraitImage != null)
            {
                var frame = EnsureBackdrop("PortraitFrame", _portraitImage.rectTransform.anchoredPosition,
                    _portraitImage.rectTransform.sizeDelta + new Vector2(32f, 32f),
                    new Color(0.18f, 0.20f, 0.26f, 0.86f), _selected.AccentColor());
                if (frame != null)
                {
                    int portraitIndex = _portraitImage.transform.GetSiblingIndex();
                    frame.SetSiblingIndex(Mathf.Max(0, portraitIndex));
                    _portraitImage.transform.SetSiblingIndex(Mathf.Min(frame.GetSiblingIndex() + 1, transform.childCount - 1));
                }
                var outline = _portraitImage.GetComponent<Outline>() ?? _portraitImage.gameObject.AddComponent<Outline>();
                outline.effectColor = GameUiChrome.Stroke;
                outline.effectDistance = new Vector2(1f, -1f);
                _portraitImage.raycastTarget = false;
            }

            GameUiChrome.ApplyReadable(_classNameText, Color.white, FontStyles.Bold);
            GameUiChrome.ApplyReadable(_traitNameText, new Color(0.96f, 0.86f, 0.56f, 1f), FontStyles.Bold);
            GameUiChrome.ApplyReadable(_traitDescText, GameUiChrome.MutedText);
            GameUiChrome.ApplyReadable(_roleText, GameUiChrome.MutedText);
            GameUiChrome.ApplyReadable(_statsText, new Color(0.92f, 0.94f, 0.98f, 1f));
            GameUiChrome.ApplyReadable(_screenTitle, new Color(1f, 0.94f, 0.66f, 1f), FontStyles.Bold);
            _roleBadgeGroup = EnsureRoleBadgeGroup();
            _profileMeterGroup = EnsureProfileMeterGroup();
            _playerSlotBadge = EnsurePlayerSlotBadge();
            _pagerPips = EnsureCharacterPagerPips();
            _focusPlaque = EnsureFocusPlaque();
            ApplyPixelActionButtons(_selected.AccentColor());

            var animator = GetComponent<CharacterSelectFaux3DAnimator>();
            if (animator == null)
                animator = gameObject.AddComponent<CharacterSelectFaux3DAnimator>();
            animator.Configure(_selected.AccentColor());

            ApplySceneLayout();
        }

        private RectTransform EnsureFocusPlaque()
        {
            var rt = EnsureContainer("CharacterFocusPlaque");
            PlaceRect(rt, new Vector2(430f, 352f), new Vector2(390f, 44f));

            var shadow = EnsureCardImage(rt, "FocusShadow", BoardVisualUtility.GetPixelCardSprite(),
                new Color(0f, 0f, 0f, 0.25f), Image.Type.Sliced);
            StretchRect(shadow.rectTransform, Vector2.zero, Vector2.one,
                new Vector2(7f, -8f), new Vector2(7f, -8f));
            shadow.transform.SetAsFirstSibling();

            var face = EnsureCardImage(rt, "FocusFace", BoardVisualUtility.GetPixelCardSprite(),
                new Color(0.12f, 0.16f, 0.23f, 0.94f), Image.Type.Sliced);
            StretchRect(face.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            var rail = EnsureCardImage(rt, "FocusRail", BoardVisualUtility.GetPixelSolidSprite(),
                new Color(0.86f, 0.68f, 0.28f, 0.92f), Image.Type.Sliced);
            StretchRect(rail.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(16f, -9f), new Vector2(-16f, -4f));

            var label = EnsureCardLabel(rt, "FocusLabel");
            label.fontSize = HudTextStyle.Scale(12f);
            label.fontSizeMin = 9f;
            label.fontSizeMax = HudTextStyle.Scale(12f);
            StretchRect(label.rectTransform, Vector2.zero, Vector2.one, new Vector2(12f, 0f), new Vector2(-12f, 0f));
            return rt;
        }

        private void UpdateFocusPlaque(CharacterType type)
        {
            if (_focusPlaque == null) return;

            Color accent = type.AccentColor();
            var face = _focusPlaque.Find("FocusFace")?.GetComponent<Image>();
            if (face != null)
                face.color = Color.Lerp(new Color(0.12f, 0.16f, 0.23f, 0.94f), accent, 0.18f);

            var rail = _focusPlaque.Find("FocusRail")?.GetComponent<Image>();
            if (rail != null)
                rail.color = new Color(accent.r, accent.g, accent.b, 0.94f);

            var label = _focusPlaque.Find("FocusLabel")?.GetComponent<TextMeshProUGUI>();
            if (label != null)
            {
                label.text = $"No.{_index + 1:00}  {type.EnglishName()} / {RoleTag(type)}";
                GameUiChrome.ApplyReadable(label, Color.white, FontStyles.Bold);
            }
        }

        private RectTransform EnsureCharacterPagerPips()
        {
            var rt = EnsureContainer("CharacterPagerPips");
            PlaceRect(rt, new Vector2(-405f, -106f), new Vector2(330f, 36f));

            var rail = EnsureCardImage(rt, "PagerRail", BoardVisualUtility.GetPixelCardSprite(),
                new Color(0.05f, 0.07f, 0.11f, 0.60f), Image.Type.Sliced);
            SetRect(rail.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), new Vector2(254f, 16f), new Vector2(0f, 0f));
            rail.transform.SetAsFirstSibling();

            var types = CharacterTypes;
            float pipCenter = (types.Length - 1) * 0.5f;
            rail.rectTransform.sizeDelta = new Vector2(Mathf.Max(120f, (types.Length - 1) * 46f + 72f), 16f);
            for (int i = 0; i < types.Length; i++)
            {
                int index = i;
                var type = types[i];
                var pip = EnsureCardImage(rt, $"PagerPip_{i}", BoardVisualUtility.GetPixelCardSprite(),
                    new Color(0.28f, 0.32f, 0.40f, 0.90f), Image.Type.Sliced);
                pip.raycastTarget = true;
                SetRect(pip.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                    new Vector2(0.5f, 0.5f), new Vector2(26f, 18f), new Vector2((i - pipCenter) * 46f, 0f));

                var sparkle = EnsureCardImage(pip.transform, "Spark", BoardVisualUtility.GetPixelSparkleSprite(),
                    new Color(type.AccentColor().r, type.AccentColor().g, type.AccentColor().b, 0.0f), Image.Type.Simple);
                SetRect(sparkle.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f),
                    new Vector2(0.5f, 0.5f), new Vector2(9f, 9f), new Vector2(-2f, -2f));

                var button = pip.GetComponent<Button>() ?? pip.gameObject.AddComponent<Button>();
                button.targetGraphic = pip;
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => SelectIndex(index));
                var colors = button.colors;
                colors.normalColor = Color.white;
                colors.highlightedColor = new Color(1.10f, 1.10f, 1.04f, 1f);
                colors.pressedColor = new Color(0.78f, 0.80f, 0.86f, 1f);
                colors.selectedColor = colors.highlightedColor;
                colors.disabledColor = new Color(0.48f, 0.50f, 0.54f, 0.50f);
                colors.fadeDuration = 0.06f;
                button.colors = colors;
            }

            return rt;
        }

        private void UpdateCharacterPagerPips()
        {
            if (_pagerPips == null) return;

            var types = CharacterTypes;
            for (int i = 0; i < types.Length; i++)
            {
                var pip = _pagerPips.Find($"PagerPip_{i}")?.GetComponent<Image>();
                if (pip == null) continue;

                bool selected = i == _index;
                Color accent = types[i].AccentColor();
                pip.sprite = BoardVisualUtility.GetPixelCardSprite();
                pip.type = Image.Type.Sliced;
                pip.color = selected
                    ? new Color(accent.r, accent.g, accent.b, 0.95f)
                    : new Color(0.22f, 0.26f, 0.34f, 0.82f);
                pip.rectTransform.sizeDelta = selected ? new Vector2(36f, 22f) : new Vector2(24f, 16f);

                var sparkle = pip.transform.Find("Spark")?.GetComponent<Image>();
                if (sparkle != null)
                    sparkle.color = new Color(accent.r, accent.g, accent.b, selected ? 0.84f : 0f);
            }
        }

        private RectTransform EnsurePlayerSlotBadge()
        {
            var rt = EnsureContainer("PlayerSlotBadge");
            PlaceRect(rt, new Vector2(-456f, 462f), new Vector2(154f, 46f));

            var shadow = EnsureCardImage(rt, "BadgeShadow", BoardVisualUtility.GetPixelCardSprite(),
                new Color(0f, 0f, 0f, 0.30f), Image.Type.Sliced);
            StretchRect(shadow.rectTransform, Vector2.zero, Vector2.one,
                new Vector2(6f, -7f), new Vector2(6f, -7f));
            shadow.transform.SetAsFirstSibling();

            var face = EnsureCardImage(rt, "BadgeFace", BoardVisualUtility.GetPixelCardSprite(),
                new Color(0.12f, 0.16f, 0.22f, 0.96f), Image.Type.Sliced);
            StretchRect(face.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            var rail = EnsureCardImage(rt, "BadgeRail", BoardVisualUtility.GetPixelSolidSprite(),
                new Color(0.86f, 0.68f, 0.28f, 0.95f), Image.Type.Sliced);
            StretchRect(rail.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(12f, -9f), new Vector2(-12f, -4f));

            var label = EnsureCardLabel(rt, "BadgeLabel");
            label.fontSize = HudTextStyle.Scale(13f);
            label.fontSizeMin = 10f;
            label.fontSizeMax = HudTextStyle.Scale(13f);
            StretchRect(label.rectTransform, Vector2.zero, Vector2.one, new Vector2(8f, 0f), new Vector2(-8f, 0f));
            return rt;
        }

        private void UpdatePlayerSlotBadge(Color slotColor)
        {
            if (_playerSlotBadge == null) return;

            var face = _playerSlotBadge.Find("BadgeFace")?.GetComponent<Image>();
            if (face != null)
                face.color = Color.Lerp(new Color(0.12f, 0.16f, 0.22f, 0.96f), slotColor, 0.22f);

            var rail = _playerSlotBadge.Find("BadgeRail")?.GetComponent<Image>();
            if (rail != null)
                rail.color = new Color(slotColor.r, slotColor.g, slotColor.b, 0.96f);

            var label = _playerSlotBadge.Find("BadgeLabel")?.GetComponent<TextMeshProUGUI>();
            if (label != null)
            {
                label.text = GameSession.HumanCount > 1
                    ? $"P{_humanSlot + 1}/{GameSession.HumanCount}"
                    : "P1";
                GameUiChrome.ApplyReadable(label, Color.white, FontStyles.Bold);
            }
        }

        private void ApplyPixelActionButtons(Color accent)
        {
            ApplyPixelActionButton(_prevButton, new Color(0.50f, 0.66f, 0.90f, 1f), primary: false);
            ApplyPixelActionButton(_nextButton, new Color(0.50f, 0.66f, 0.90f, 1f), primary: false);
            ApplyPixelActionButton(_backButton, new Color(0.72f, 0.76f, 0.82f, 1f), primary: false);
            ApplyPixelActionButton(_confirmButton, accent, primary: true);
        }

        private static void ApplyPixelActionButton(Button button, Color accent, bool primary)
        {
            if (button == null) return;

            var rt = button.GetComponent<RectTransform>();
            var img = button.GetComponent<Image>() ?? button.gameObject.AddComponent<Image>();
            img.sprite = BoardVisualUtility.GetPixelSolidSprite();
            img.type = Image.Type.Sliced;
            img.color = new Color(1f, 1f, 1f, 0.01f);
            img.raycastTarget = true;

            var face = EnsureCardImage(button.transform, "PixelButtonFace", BoardVisualUtility.GetPixelCardSprite(),
                primary
                    ? Color.Lerp(new Color(0.16f, 0.20f, 0.28f, 0.98f), accent, 0.38f)
                    : Color.Lerp(new Color(0.14f, 0.18f, 0.26f, 0.96f), accent, 0.16f),
                Image.Type.Sliced);
            StretchRect(face.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            button.targetGraphic = face;

            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = primary
                ? new Color(1.10f, 1.08f, 0.96f, 1f)
                : new Color(1.08f, 1.08f, 1.08f, 1f);
            colors.pressedColor = primary
                ? Color.Lerp(new Color(0.11f, 0.12f, 0.16f, 1f), accent, 0.28f)
                : new Color(0.12f, 0.14f, 0.20f, 1f);
            colors.selectedColor = colors.highlightedColor;
            colors.disabledColor = new Color(0.42f, 0.44f, 0.48f, 0.58f);
            colors.fadeDuration = 0.07f;
            button.colors = colors;

            Vector2 size = rt != null ? rt.sizeDelta : new Vector2(160f, 50f);
            var shadow = EnsureCardImage(button.transform, "PixelButtonShadow", BoardVisualUtility.GetPixelCardSprite(),
                new Color(0f, 0f, 0f, 0.28f), Image.Type.Sliced);
            StretchRect(shadow.rectTransform, Vector2.zero, Vector2.one,
                new Vector2(6f, -8f), new Vector2(6f, -8f));

            var right = EnsureCardImage(button.transform, "PixelButtonRightDepth", BoardVisualUtility.GetPixelSolidSprite(),
                Color.Lerp(Color.black, accent, primary ? 0.32f : 0.20f), Image.Type.Sliced);
            SetRect(right.rectTransform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
                new Vector2(1f, 0.5f), new Vector2(8f, Mathf.Max(28f, size.y - 12f)), new Vector2(6f, -5f));

            var bottom = EnsureCardImage(button.transform, "PixelButtonBottomDepth", BoardVisualUtility.GetPixelSolidSprite(),
                Color.Lerp(Color.black, accent, primary ? 0.28f : 0.18f), Image.Type.Sliced);
            SetRect(bottom.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f), new Vector2(Mathf.Max(34f, size.x - 20f), 8f), new Vector2(5f, -7f));

            var top = EnsureCardImage(button.transform, "PixelButtonTopRule", BoardVisualUtility.GetPixelSolidSprite(),
                new Color(accent.r, accent.g, accent.b, primary ? 0.96f : 0.58f), Image.Type.Sliced);
            StretchRect(top.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(14f, -9f), new Vector2(-14f, -4f));

            shadow.transform.SetSiblingIndex(0);
            right.transform.SetSiblingIndex(1);
            bottom.transform.SetSiblingIndex(2);
            face.transform.SetSiblingIndex(3);
            top.transform.SetSiblingIndex(4);

            foreach (var tmp in button.GetComponentsInChildren<TextMeshProUGUI>(true))
            {
                if (tmp.transform.name.StartsWith("PixelButton", System.StringComparison.Ordinal)) continue;
                JapaneseFontProvider.Apply(tmp);
                tmp.color = primary ? Color.white : new Color(0.92f, 0.95f, 1f, 1f);
                tmp.fontStyle |= FontStyles.Bold;
                tmp.raycastTarget = false;
                tmp.transform.SetAsLastSibling();
                HudTextStyle.ApplyOutlineSafe(tmp, 0.12f, new Color(0f, 0f, 0f, 0.78f));
            }
        }

        private RectTransform EnsureRoleBadgeGroup()
        {
            var rt = EnsureContainer("CharacterRoleBadgeGroup");
            PlaceRect(rt, new Vector2(430f, -40f), new Vector2(470f, 34f));

            EnsureRoleBadge(rt, "PrimaryRoleBadge", new Vector2(-104f, 0f), new Vector2(204f, 28f));
            EnsureRoleBadge(rt, "RiskRoleBadge", new Vector2(112f, 0f), new Vector2(204f, 28f));
            return rt;
        }

        private void EnsureRoleBadge(RectTransform parent, string name, Vector2 pos, Vector2 size)
        {
            var badge = EnsureCardImage(parent, name, BoardVisualUtility.GetPixelCardSprite(),
                new Color(0.16f, 0.20f, 0.28f, 0.92f), Image.Type.Sliced);
            SetRect(badge.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), size, pos);

            var label = EnsureCardLabel(badge.transform, "Label");
            label.fontSize = HudTextStyle.Scale(11f);
            label.fontSizeMin = 8f;
            label.fontSizeMax = HudTextStyle.Scale(11f);
            StretchRect(label.rectTransform, Vector2.zero, Vector2.one, new Vector2(8f, 0f), new Vector2(-8f, 0f));
        }

        private RectTransform EnsureProfileMeterGroup()
        {
            var rt = EnsureContainer("ProfileMeterGroup");
            PlaceRect(rt, new Vector2(430f, -246f), new Vector2(470f, 116f));

            EnsureMeterRow(rt, "Money", 42f);
            EnsureMeterRow(rt, "Mental", 14f);
            EnsureMeterRow(rt, "Stability", -14f);
            EnsureMeterRow(rt, "Burst", -42f);
            return rt;
        }

        private void EnsureMeterRow(RectTransform parent, string id, float y)
        {
            var row = EnsureContainer(parent, $"MeterRow_{id}");
            SetRect(row, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), new Vector2(430f, 24f), new Vector2(0f, y));

            var label = EnsureCardLabel(row, "Label");
            label.fontSize = HudTextStyle.Scale(10f);
            label.fontSizeMin = 8f;
            label.fontSizeMax = HudTextStyle.Scale(10f);
            label.alignment = TextAlignmentOptions.Left;
            SetRect(label.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), new Vector2(92f, 22f), new Vector2(-168f, 0f));

            var track = EnsureCardImage(row, "Track", BoardVisualUtility.GetPixelCardSprite(),
                new Color(0.04f, 0.06f, 0.10f, 0.78f), Image.Type.Sliced);
            SetRect(track.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), new Vector2(230f, 14f), new Vector2(12f, 0f));

            var fill = EnsureCardImage(row, "Fill", BoardVisualUtility.GetPixelSolidSprite(),
                Color.white, Image.Type.Sliced);
            SetRect(fill.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, 0.5f), new Vector2(12f, 8f), new Vector2(-103f, 0f));

            var cap = EnsureCardImage(row, "Cap", BoardVisualUtility.GetPixelSparkleSprite(),
                new Color(1f, 1f, 1f, 0.38f), Image.Type.Simple);
            SetRect(cap.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), new Vector2(10f, 10f), new Vector2(-96f, 0f));

            var value = EnsureCardLabel(row, "Value");
            value.fontSize = HudTextStyle.Scale(10f);
            value.fontSizeMin = 8f;
            value.fontSizeMax = HudTextStyle.Scale(10f);
            value.alignment = TextAlignmentOptions.Right;
            SetRect(value.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), new Vector2(82f, 22f), new Vector2(184f, 0f));
        }

        private RectTransform EnsureContainer(string name)
        {
            var child = transform.Find(name);
            if (child == null)
            {
                var go = new GameObject(name, typeof(RectTransform), typeof(LayoutElement));
                go.transform.SetParent(transform, false);
                go.GetComponent<LayoutElement>().ignoreLayout = true;
                return go.GetComponent<RectTransform>();
            }

            var le = child.GetComponent<LayoutElement>() ?? child.gameObject.AddComponent<LayoutElement>();
            le.ignoreLayout = true;
            return child as RectTransform;
        }

        private static RectTransform EnsureContainer(Transform parent, string name)
        {
            var child = parent.Find(name);
            if (child == null)
            {
                var go = new GameObject(name, typeof(RectTransform), typeof(LayoutElement));
                go.transform.SetParent(parent, false);
                go.GetComponent<LayoutElement>().ignoreLayout = true;
                return go.GetComponent<RectTransform>();
            }

            var le = child.GetComponent<LayoutElement>() ?? child.gameObject.AddComponent<LayoutElement>();
            le.ignoreLayout = true;
            return child as RectTransform;
        }

        private void UpdateRoleBadges(CharacterType type)
        {
            if (_roleBadgeGroup == null) return;

            Color accent = type.AccentColor();
            SetBadge("PrimaryRoleBadge", RoleTag(type), accent);
            SetBadge("RiskRoleBadge", RiskTag(type), RiskColor(type, accent));
        }

        private void SetBadge(string name, string text, Color color)
        {
            var badge = _roleBadgeGroup.Find(name)?.GetComponent<Image>();
            if (badge != null)
            {
                badge.sprite = BoardVisualUtility.GetPixelCardSprite();
                badge.type = Image.Type.Sliced;
                badge.color = new Color(color.r * 0.34f, color.g * 0.34f, color.b * 0.34f, 0.94f);
            }

            var label = _roleBadgeGroup.Find($"{name}/Label")?.GetComponent<TextMeshProUGUI>();
            if (label != null)
            {
                label.text = text;
                GameUiChrome.ApplyReadable(label, Color.white, FontStyles.Bold);
            }
        }

        private void UpdateProfileMeters(CharacterType type, CharacterProfile profile)
        {
            if (_profileMeterGroup == null) return;

            Color accent = type.AccentColor();
            UpdateMeterRow("Money", "資金", Mathf.Clamp01(profile.Money / 50f),
                $"{profile.Money}万", new Color(1f, 0.82f, 0.24f, 1f));
            UpdateMeterRow("Mental", "気力", Mathf.Clamp01(profile.Mental / 50f),
                $"{profile.Mental}/{profile.MaxMental}", new Color(0.44f, 0.86f, 1f, 1f));
            UpdateMeterRow("Stability", "安定", CharacterStability(type) / 100f,
                $"{CharacterStability(type)}%", new Color(0.44f, 0.86f, 0.46f, 1f));
            UpdateMeterRow("Burst", "爆発", CharacterBurst(type) / 100f,
                $"{CharacterBurst(type)}%", accent);
        }

        private void UpdateMeterRow(string id, string labelText, float normalized, string valueText, Color color)
        {
            var row = _profileMeterGroup.Find($"MeterRow_{id}");
            if (row == null) return;

            normalized = Mathf.Clamp01(normalized);

            var label = row.Find("Label")?.GetComponent<TextMeshProUGUI>();
            if (label != null)
            {
                label.text = labelText;
                GameUiChrome.ApplyReadable(label, new Color(0.86f, 0.90f, 0.96f, 1f), FontStyles.Bold);
            }

            var track = row.Find("Track")?.GetComponent<Image>();
            if (track != null)
                track.color = new Color(0.04f, 0.06f, 0.10f, 0.80f);

            var fill = row.Find("Fill")?.GetComponent<Image>();
            if (fill != null)
            {
                fill.color = new Color(color.r, color.g, color.b, 0.92f);
                fill.rectTransform.sizeDelta = new Vector2(Mathf.Lerp(12f, 222f, normalized), 8f);
            }

            var cap = row.Find("Cap")?.GetComponent<Image>();
            if (cap != null)
            {
                cap.color = new Color(color.r, color.g, color.b, 0.74f);
                cap.rectTransform.anchoredPosition = new Vector2(Mathf.Lerp(-96f, 112f, normalized), 0f);
            }

            var value = row.Find("Value")?.GetComponent<TextMeshProUGUI>();
            if (value != null)
            {
                value.text = valueText;
                GameUiChrome.ApplyReadable(value, Color.white, FontStyles.Bold);
            }
        }

        private static int CharacterStability(CharacterType type) => type switch
        {
            CharacterType.Hobbyist => 92,
            CharacterType.Serious  => 74,
            CharacterType.Athletic => 84,
            CharacterType.Rich     => 78,
            CharacterType.Genius   => 42,
            _                      => 60
        };

        private static int CharacterBurst(CharacterType type) => type switch
        {
            CharacterType.Hobbyist => 56,
            CharacterType.Serious  => 66,
            CharacterType.Athletic => 72,
            CharacterType.Rich     => 70,
            CharacterType.Genius   => 96,
            _                      => 60
        };

        private static string RoleTag(CharacterType type) => type switch
        {
            CharacterType.Hobbyist => "安定回避型",
            CharacterType.Serious  => "IF堅実型",
            CharacterType.Athletic => "高負荷突破型",
            CharacterType.Rich     => "資金解決型",
            CharacterType.Genius   => "爆発研究型",
            _                      => "標準型"
        };

        private static string RiskTag(CharacterType type) => type switch
        {
            CharacterType.Hobbyist => "リスク低",
            CharacterType.Serious  => "突発注意",
            CharacterType.Athletic => "事故耐性",
            CharacterType.Rich     => "金欠注意",
            CharacterType.Genius   => "リスク高",
            _                      => "標準"
        };

        private static Color RiskColor(CharacterType type, Color accent) => type switch
        {
            CharacterType.Genius   => new Color(1f, 0.34f, 0.38f, 1f),
            CharacterType.Serious  => new Color(1f, 0.74f, 0.32f, 1f),
            CharacterType.Rich     => new Color(1f, 0.80f, 0.24f, 1f),
            CharacterType.Hobbyist => new Color(0.42f, 0.90f, 0.56f, 1f),
            _                      => accent
        };

        private static CharacterType CharacterAt(int index)
        {
            if (CharacterTypes.Length == 0) return default;
            return CharacterTypes[NormalizeCharacterIndex(index)];
        }

        private static int NormalizeCharacterIndex(int index)
        {
            int count = CharacterTypes.Length;
            if (count <= 0) return 0;

            int wrapped = index % count;
            return wrapped < 0 ? wrapped + count : wrapped;
        }

        private static int IndexOfCharacter(CharacterType type)
        {
            for (int i = 0; i < CharacterTypes.Length; i++)
                if (CharacterTypes[i] == type)
                    return i;
            return 0;
        }

        private RectTransform EnsureBackdrop(string name, Vector2 anchoredPosition, Vector2 size, Color surface, Color accent)
        {
            var parent = transform;
            var existing = parent.Find(name);
            RectTransform rt;
            if (existing == null)
            {
                var go = new GameObject(name, typeof(RectTransform), typeof(Image));
                go.transform.SetParent(parent, false);
                rt = go.GetComponent<RectTransform>();
            }
            else
            {
                rt = existing as RectTransform;
                if (rt == null) return null;
            }

            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = anchoredPosition;
            rt.sizeDelta = size;

            var img = rt.GetComponent<Image>() ?? rt.gameObject.AddComponent<Image>();
            img.sprite = BoardVisualUtility.GetPixelCardSprite();
            img.type = Image.Type.Sliced;
            img.color = surface;
            img.raycastTarget = false;

            var outline = rt.GetComponent<Outline>() ?? rt.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(1f, 1f, 1f, 0.22f);
            outline.effectDistance = new Vector2(2f, -2f);
            outline.useGraphicAlpha = true;

            var top = EnsureCardImage(rt, "PixelPanelTopRule", BoardVisualUtility.GetPixelSolidSprite(), accent, Image.Type.Sliced);
            StretchRect(top.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(22f, -13f), new Vector2(-22f, -5f));

            var right = EnsureCardImage(rt, "PixelPanelRightDepth", BoardVisualUtility.GetPixelSolidSprite(),
                Color.Lerp(Color.black, accent, 0.20f), Image.Type.Sliced);
            StretchRect(right.rectTransform, new Vector2(1f, 0f), new Vector2(1f, 1f),
                new Vector2(7f, 18f), new Vector2(15f, -22f));

            var bottom = EnsureCardImage(rt, "PixelPanelBottomDepth", BoardVisualUtility.GetPixelSolidSprite(),
                Color.Lerp(Color.black, accent, 0.18f), Image.Type.Sliced);
            StretchRect(bottom.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f),
                new Vector2(18f, -15f), new Vector2(-22f, -7f));

            GameUiChrome.ApplyAccentRail(rt, accent, 5f);
            return rt;
        }

        private void UpdateScreenAccent(Color accent)
        {
            SetImageColor("CharacterSelectAccentPlate", new Color(accent.r, accent.g, accent.b, 0.78f));
            SetImageColor("CharacterInfoPanel/PixelPanelTopRule", new Color(accent.r, accent.g, accent.b, 0.96f));
            SetImageColor("CharacterInfoPanel/AccentRail", new Color(accent.r, accent.g, accent.b, 0.96f));
            SetImageColor("PortraitFrame/PixelPanelTopRule", new Color(accent.r, accent.g, accent.b, 1f));
            SetImageColor("PortraitFrame/AccentRail", new Color(accent.r, accent.g, accent.b, 1f));
            GetComponent<CharacterSelectFaux3DAnimator>()?.SetAccent(accent);
        }

        private void SetImageColor(string path, Color color)
        {
            var img = transform.Find(path)?.GetComponent<Image>();
            if (img != null)
                img.color = color;
        }

        private static void StyleCharacterCardShell(Transform card, Color accent, bool selected)
        {
            if (card == null) return;

            var hitArea = card.GetComponent<Image>();
            if (hitArea != null)
            {
                hitArea.sprite = BoardVisualUtility.GetPixelSolidSprite();
                hitArea.type = Image.Type.Sliced;
                hitArea.color = new Color(1f, 1f, 1f, 0.01f);
                hitArea.raycastTarget = true;
            }

            var buttonAccent = card.Find("ButtonAccent")?.GetComponent<Image>();
            if (buttonAccent != null)
                buttonAccent.color = Color.clear;

            var outline = card.GetComponent<Outline>() ?? card.gameObject.AddComponent<Outline>();
            outline.effectColor = selected ? new Color(1f, 0.92f, 0.62f, 0.30f) : Color.clear;
            outline.effectDistance = selected ? new Vector2(3f, -3f) : Vector2.zero;

            var halo = card.Find("SelectedCardHalo")?.GetComponent<Image>();
            if (halo != null)
            {
                halo.sprite = BoardVisualUtility.GetSoftOvalShadowSprite();
                halo.type = Image.Type.Simple;
                halo.color = new Color(accent.r, accent.g, accent.b, selected ? 0.30f : 0.00f);
            }

            var face = card.Find("CardFace")?.GetComponent<Image>();
            if (face != null)
            {
                face.sprite = BoardVisualUtility.GetPixelCardSprite();
                face.type = Image.Type.Sliced;
                face.color = selected
                    ? Color.Lerp(new Color(0.16f, 0.22f, 0.31f, 0.98f), accent, 0.30f)
                    : new Color(0.12f, 0.16f, 0.23f, 0.96f);

                var button = card.GetComponent<Button>();
                if (button != null)
                    button.targetGraphic = face;
            }

            var shadow = card.Find("CardDepthShadow")?.GetComponent<Image>();
            if (shadow != null)
            {
                shadow.color = new Color(0f, 0f, 0f, selected ? 0.38f : 0.24f);
                StretchRect(shadow.rectTransform, Vector2.zero, Vector2.one,
                    selected ? new Vector2(14f, -18f) : new Vector2(10f, -14f),
                    selected ? new Vector2(14f, -18f) : new Vector2(10f, -14f));
            }

            var topRule = card.Find("CardTopRule")?.GetComponent<Image>();
            if (topRule != null)
                topRule.color = new Color(accent.r, accent.g, accent.b, selected ? 1f : 0.64f);

            var numberBadge = card.Find("CharacterNumberBadge")?.GetComponent<Image>();
            if (numberBadge != null)
            {
                numberBadge.sprite = BoardVisualUtility.GetPixelCardSprite();
                numberBadge.type = Image.Type.Sliced;
                numberBadge.color = selected
                    ? new Color(accent.r * 0.64f, accent.g * 0.64f, accent.b * 0.64f, 0.98f)
                    : new Color(accent.r * 0.34f, accent.g * 0.34f, accent.b * 0.34f, 0.76f);
                numberBadge.rectTransform.sizeDelta = selected ? new Vector2(40f, 24f) : new Vector2(34f, 21f);
            }

            var numberLabel = card.Find("CharacterNumberBadge/Label")?.GetComponent<TextMeshProUGUI>();
            if (numberLabel != null)
                GameUiChrome.ApplyReadable(numberLabel,
                    selected ? Color.white : new Color(0.84f, 0.88f, 0.94f, 0.92f), FontStyles.Bold);

            var selectedTab = card.Find("SelectedTab")?.GetComponent<Image>();
            if (selectedTab != null)
            {
                selectedTab.sprite = BoardVisualUtility.GetPixelCardSprite();
                selectedTab.type = Image.Type.Sliced;
                selectedTab.color = new Color(accent.r * 0.58f, accent.g * 0.58f, accent.b * 0.58f,
                    selected ? 0.98f : 0f);
                selectedTab.gameObject.SetActive(selected);
            }

            var selectedTabLabel = card.Find("SelectedTab/Label")?.GetComponent<TextMeshProUGUI>();
            if (selectedTabLabel != null)
            {
                selectedTabLabel.text = "選択中";
                selectedTabLabel.gameObject.SetActive(selected);
                GameUiChrome.ApplyReadable(selectedTabLabel,
                    selected ? Color.white : new Color(1f, 1f, 1f, 0f), FontStyles.Bold);
            }

            var right = card.Find("CardRightExtrude")?.GetComponent<Image>();
            if (right != null)
                right.color = Color.Lerp(Color.black, accent, selected ? 0.34f : 0.20f);

            var bottom = card.Find("CardBottomExtrude")?.GetComponent<Image>();
            if (bottom != null)
                bottom.color = Color.Lerp(Color.black, accent, selected ? 0.30f : 0.18f);

            var window = card.Find("PortraitWindow")?.GetComponent<Image>();
            if (window != null)
            {
                window.sprite = BoardVisualUtility.GetPixelCardSprite();
                window.type = Image.Type.Sliced;
                window.color = selected
                    ? new Color(0.05f, 0.08f, 0.14f, 0.98f)
                    : new Color(0.08f, 0.10f, 0.15f, 0.92f);
            }

            var roleRibbon = card.Find("RoleRibbon")?.GetComponent<Image>();
            if (roleRibbon != null)
            {
                roleRibbon.sprite = BoardVisualUtility.GetPixelCardSprite();
                roleRibbon.type = Image.Type.Sliced;
                roleRibbon.color = selected
                    ? new Color(accent.r * 0.62f, accent.g * 0.62f, accent.b * 0.62f, 0.96f)
                    : new Color(accent.r * 0.30f, accent.g * 0.30f, accent.b * 0.30f, 0.76f);
                roleRibbon.rectTransform.sizeDelta = selected ? new Vector2(104f, 21f) : new Vector2(92f, 18f);
            }

            var roleRibbonLabel = card.Find("RoleRibbon/Label")?.GetComponent<TextMeshProUGUI>();
            if (roleRibbonLabel != null)
                GameUiChrome.ApplyReadable(roleRibbonLabel,
                    selected ? Color.white : new Color(0.86f, 0.90f, 0.96f, 0.92f), FontStyles.Bold);

            var chip = card.Find("TraitChip")?.GetComponent<Image>();
            if (chip != null)
            {
                chip.sprite = BoardVisualUtility.GetPixelCardSprite();
                chip.type = Image.Type.Sliced;
                chip.color = selected
                    ? new Color(accent.r * 0.58f, accent.g * 0.58f, accent.b * 0.58f, 0.96f)
                    : new Color(accent.r * 0.28f, accent.g * 0.28f, accent.b * 0.28f, 0.86f);
            }

            var meter = card.Find("SelectionMeter")?.GetComponent<Image>();
            if (meter != null)
            {
                meter.color = new Color(accent.r, accent.g, accent.b, selected ? 0.95f : 0.30f);
                meter.rectTransform.sizeDelta = new Vector2(selected ? 116f : 86f, 7f);
            }

            var sparkleA = card.Find("CardSparkleA")?.GetComponent<Image>();
            if (sparkleA != null)
                sparkleA.color = selected ? new Color(1f, 1f, 1f, 0.74f) : new Color(1f, 1f, 1f, 0.26f);

            var sparkleB = card.Find("CardSparkleB")?.GetComponent<Image>();
            if (sparkleB != null)
                sparkleB.color = new Color(accent.r, accent.g, accent.b, selected ? 0.74f : 0.24f);

            var label = card.Find("Label")?.GetComponent<TextMeshProUGUI>();
            if (label != null)
                GameUiChrome.ApplyReadable(label, selected ? Color.white : new Color(0.88f, 0.90f, 0.94f, 1f), FontStyles.Bold);

            var trait = card.Find("TraitChip/TraitText")?.GetComponent<TextMeshProUGUI>();
            if (trait != null)
                GameUiChrome.ApplyReadable(trait, selected ? Color.white : new Color(0.82f, 0.86f, 0.92f, 1f), FontStyles.Bold);
        }

        private void OnConfirm()
        {
            if (_confirming) return;
            _confirming = true;
            SetInteractable(false);

            GameSession.SetHumanCharacter(_humanSlot, _selected);

            if (_humanSlot < GameSession.HumanCount - 1)
            {
                _humanSlot++;
                _selected = GameSession.GetHumanCharacter(_humanSlot);
                _index    = IndexOfCharacter(_selected);
                _lastJuiceIndex = -1;
                _confirming = false;
                SetInteractable(true);
                RefreshPanel(playJuice: false);
                return;
            }

            GameManager.HumanCount     = GameSession.HumanCount;
            GameManager.CpuCount       = GameSession.CpuCount;
            GameManager.HumanCharacter = GameSession.GetHumanCharacter(0);

            RectTransform cardRt = null;
            if (_cardButtons != null && _index >= 0 && _index < _cardButtons.Length && _cardButtons[_index] != null)
                cardRt = _cardButtons[_index].GetComponent<RectTransform>();

            SceneTransition.LoadGameWorldAfterConfirm(_juice, cardRt, _selected);
        }

        private void SetInteractable(bool on)
        {
            if (_confirmButton != null) _confirmButton.interactable = on;
            if (_prevButton != null) _prevButton.interactable = on;
            if (_nextButton != null) _nextButton.interactable = on;
            if (_backButton != null) _backButton.interactable = on;
            if (_cardButtons == null) return;
            foreach (var b in _cardButtons)
                if (b != null) b.interactable = on;
        }
    }
}
