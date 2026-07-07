using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Sugoroku.Board;

namespace Sugoroku.UI
{
    /// <summary>「全体を見る」トグルボタンの表示とカメラ連携。</summary>
    [DisallowMultipleComponent]
    public class BoardOverviewUi : MonoBehaviour
    {
        private Button _button;
        private TextMeshProUGUI _label;

        private void Awake()
        {
            Bind();
            if (_button != null)
                _button.onClick.AddListener(OnToggleClicked);
        }

        private void Start() => Bind();

        private void OnDestroy()
        {
            if (_button != null)
                _button.onClick.RemoveListener(OnToggleClicked);
        }

        private void Bind()
        {
            var root = transform.Find("OverviewButton");
            if (root == null)
            {
                var canvas = GetComponent<Canvas>() ?? GetComponentInParent<Canvas>();
                root = canvas != null ? canvas.transform.Find("OverviewButton") : null;
            }

            if (root == null) return;

            _button = root.GetComponent<Button>();
            _label  = root.GetComponentInChildren<TextMeshProUGUI>(true);
        }

        private void OnToggleClicked()
        {
            var cam = BoardCameraController.Instance;
            if (cam == null) return;

            cam.ToggleOverview();
            RefreshLabel(cam.IsOverviewMode);
        }

        private void RefreshLabel(bool overview)
        {
            if (_label != null)
                _label.text = overview ? "フォーカスに戻る" : "全体を見る";
        }
    }

}
