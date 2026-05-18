using TMPro;
using UnityEngine;

namespace Sugoroku.UI
{
    /// <summary>
    /// TMP の Awake より先に日本語フォントを割り当てる（LiberationSans 警告の防止）。
    /// </summary>
    [DefaultExecutionOrder(-10000)]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(TextMeshProUGUI))]
    public sealed class JapaneseFontEnforcer : MonoBehaviour
    {
        private TextMeshProUGUI _text;

        private void Awake()
        {
            _text = GetComponent<TextMeshProUGUI>();
            JapaneseFontProvider.WarmUpAndSetDefault();
            JapaneseFontProvider.Apply(_text);
        }

        private void OnEnable()
        {
            if (_text == null) _text = GetComponent<TextMeshProUGUI>();
            JapaneseFontProvider.Apply(_text);
        }
    }
}
