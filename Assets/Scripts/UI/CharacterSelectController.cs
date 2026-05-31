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

namespace Sugoroku.UI
{
    public class CharacterSelectController : MonoBehaviour
    {
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
            _index     = (int)_selected;
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
                bg.color = new Color(0.09f, 0.11f, 0.16f, 0.98f);
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
            _cardPreferredSize = new Vector2(126f, 198f);

            PlaceRect(_cardParent, new Vector2(-405f, 44f), new Vector2(780f, 238f));
            if (_cardParent != null)
            {
                var h = _cardParent.GetComponent<HorizontalLayoutGroup>() ?? _cardParent.gameObject.AddComponent<HorizontalLayoutGroup>();
                h.spacing = 18f;
                h.childAlignment = TextAnchor.MiddleCenter;
                h.childControlWidth = true;
                h.childControlHeight = true;
                h.childForceExpandWidth = false;
                h.childForceExpandHeight = false;
            }

            PlaceButton(_prevButton, new Vector2(-830f, 44f), new Vector2(64f, 64f), HudTextStyle.Scale(22f));
            PlaceButton(_nextButton, new Vector2(20f, 44f), new Vector2(64f, 64f), HudTextStyle.Scale(22f));
            PlaceButton(_backButton, new Vector2(-840f, 462f), new Vector2(160f, 50f), HudTextStyle.Scale(15f));
            PlaceButton(_confirmButton, new Vector2(430f, -338f), new Vector2(300f, 62f), HudTextStyle.Scale(16f));

            PlaceText(_screenTitle, new Vector2(0f, 462f), new Vector2(760f, 52f), HudTextStyle.Scale(24f),
                TextAlignmentOptions.Center);

            PlaceImage(_portraitImage, new Vector2(430f, 250f), new Vector2(168f, 168f));
            PlaceText(_classNameText, new Vector2(430f, 146f), new Vector2(470f, 46f), HudTextStyle.Scale(24f),
                TextAlignmentOptions.Center);
            PlaceText(_traitNameText, new Vector2(430f, 100f), new Vector2(470f, 40f), HudTextStyle.Scale(16f),
                TextAlignmentOptions.Center);
            PlaceText(_traitDescText, new Vector2(430f, 30f), new Vector2(470f, 88f), HudTextStyle.Scale(14f),
                TextAlignmentOptions.TopLeft, wrap: true);
            PlaceText(_roleText, new Vector2(430f, -86f), new Vector2(470f, 104f), HudTextStyle.Scale(13f),
                TextAlignmentOptions.TopLeft, wrap: true);
            PlaceText(_statsText, new Vector2(430f, -220f), new Vector2(470f, 132f), HudTextStyle.Scale(14f),
                TextAlignmentOptions.TopLeft, wrap: true);
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

            var types = (CharacterType[])System.Enum.GetValues(typeof(CharacterType));
            _cardButtons = new Button[types.Length];
            for (int i = 0; i < types.Length; i++)
            {
                int idx = i;
                var type = types[i];
                var go = new GameObject(type.DisplayName(), typeof(RectTransform));
                go.transform.SetParent(_cardParent, false);

                var img = go.AddComponent<Image>();
                var portrait = OriginalcharAssets.GetSprite(type);
                if (portrait != null)
                {
                    img.sprite = portrait;
                    img.color  = Color.white;
                    img.preserveAspect = true;
                }
                else
                    img.color = new Color(0.15f, 0.16f, 0.28f, 0.95f);

                var btn = go.AddComponent<Button>();
                var rt = go.GetComponent<RectTransform>();
                if (layout != null)
                {
                    var le = go.AddComponent<LayoutElement>();
                    le.preferredWidth  = _cardPreferredSize.x;
                    le.preferredHeight = _cardPreferredSize.y;
                }
                else
                    rt.sizeDelta = _cardPreferredSize;

                var label = new GameObject("Label", typeof(RectTransform));
                label.transform.SetParent(go.transform, false);
                var tmp = label.AddComponent<TextMeshProUGUI>();
                tmp.text = type.DisplayName();
                tmp.fontSize = HudTextStyle.Scale(13f);
                tmp.enableAutoSizing = true;
                tmp.fontSizeMin = 12f;
                tmp.fontSizeMax = HudTextStyle.Scale(13f);
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.color = Color.white;
                tmp.raycastTarget = false;
                TitleMenuController.ApplyJapaneseFont(tmp);
                var lrt = label.GetComponent<RectTransform>();
                lrt.anchorMin = new Vector2(0, 0);
                lrt.anchorMax = new Vector2(1, 0.30f);
                lrt.offsetMin = lrt.offsetMax = Vector2.zero;
                StyleCharacterCardShell(go.transform, type.AccentColor(), selected: false);

                btn.onClick.AddListener(() => SelectIndex(idx, type));
                var hover = go.AddComponent<CharacterSelectCardHover>();
                hover.Setup(this, idx);
                _cardButtons[i] = btn;
            }
        }

        public void OnCardHover(int index)
        {
            if (_confirming || index == _index) return;
            SelectIndex(index, (CharacterType)index, fromHover: true);
        }

        private void SelectIndex(int index, CharacterType type, bool fromHover = false)
        {
            _index    = index;
            _selected = type;
            RefreshPanel(playJuice: true, isNewSelection: !fromHover || _lastJuiceIndex != index);
        }

        private void WireButtons()
        {
            if (_prevButton != null)
            {
                _prevButton.onClick.RemoveAllListeners();
                _prevButton.onClick.AddListener(() => SelectIndex((_index + 4) % 5, (CharacterType)((_index + 4) % 5)));
            }
            if (_nextButton != null)
            {
                _nextButton.onClick.RemoveAllListeners();
                _nextButton.onClick.AddListener(() => SelectIndex((_index + 1) % 5, (CharacterType)((_index + 1) % 5)));
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
                _statsText.text =
                    $"【初期ステータス】\n" +
                    $"所持金: {p.Money} 万円\n" +
                    $"IF: {p.IfScore} pt\n" +
                    $"メンタル: {p.Mental} / {p.MaxMental}" +
                    (_selected == CharacterType.Genius ? " (低い!)" : "") + "\n" +
                    $"徳: {p.Virtue} pt";

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

            if (_cardButtons == null) return;

            var types = (CharacterType[])System.Enum.GetValues(typeof(CharacterType));
            for (int i = 0; i < _cardButtons.Length; i++)
            {
                if (_cardButtons[i] == null) continue;
                var img = _cardButtons[i].GetComponent<Image>();
                bool sel = i == _index;
                var spr = OriginalcharAssets.GetSprite(types[i]);
                if (spr != null)
                {
                    img.sprite = spr;
                    img.preserveAspect = true;
                    img.color = sel ? Color.white : new Color(0.76f, 0.78f, 0.82f, 1f);
                }
                else
                    img.color = sel ? _selected.AccentColor() : new Color(0.15f, 0.16f, 0.28f, 0.95f);

                StyleCharacterCardShell(_cardButtons[i].transform, types[i].AccentColor(), sel);
                _cardButtons[i].transform.localScale = sel ? Vector3.one * 1.08f : Vector3.one;

                if (sel && playJuice && isNewSelection && _lastJuiceIndex != i)
                {
                    _juice?.PlaySelectionChanged(_cardButtons[i].transform, isNewSelection);
                    _lastJuiceIndex = i;
                }
            }
        }

        private void ApplyScreenChrome()
        {
            ApplySceneLayout();

            var deckPanel = EnsureBackdrop("CardDeckPanel", new Vector2(-405f, 44f), new Vector2(890f, 300f),
                new Color(0.12f, 0.14f, 0.20f, 0.78f), new Color(0.50f, 0.66f, 0.88f, 0.62f));
            var infoPanel = EnsureBackdrop("CharacterInfoPanel", new Vector2(430f, 20f), new Vector2(560f, 760f),
                new Color(0.12f, 0.15f, 0.21f, 0.86f), new Color(0.86f, 0.68f, 0.28f, 0.86f));
            if (deckPanel != null) deckPanel.SetAsFirstSibling();
            if (infoPanel != null) infoPanel.SetSiblingIndex(Mathf.Min(1, transform.childCount - 1));

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
            ApplySceneLayout();
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
            GameUiChrome.ApplySurface(rt, surface);
            GameUiChrome.ApplyAccentRail(rt, accent, 5f);
            return rt;
        }

        private static void StyleCharacterCardShell(Transform card, Color accent, bool selected)
        {
            if (card == null) return;

            var outline = card.GetComponent<Outline>() ?? card.gameObject.AddComponent<Outline>();
            outline.effectColor = selected ? new Color(1f, 0.92f, 0.62f, 0.92f) : GameUiChrome.Stroke;
            outline.effectDistance = selected ? new Vector2(2f, -2f) : new Vector2(1f, -1f);

            GameUiChrome.ApplyLabelBand(card, selected
                ? new Color(accent.r * 0.52f, accent.g * 0.52f, accent.b * 0.52f, 0.94f)
                : new Color(0.08f, 0.09f, 0.12f, 0.88f));

            var label = card.Find("Label")?.GetComponent<TextMeshProUGUI>();
            if (label != null)
                GameUiChrome.ApplyReadable(label, selected ? Color.white : new Color(0.88f, 0.90f, 0.94f, 1f), FontStyles.Bold);
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
                _index    = (int)_selected;
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
