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
        }

        private void Start() => Bind();

        private void OnEnable() => Bind();

        private void Update()
        {
            if (_button == null) Bind();
        }

        private void OnDestroy()
        {
            if (_button != null)
                _button.onClick.RemoveListener(OnToggleClicked);
        }

        private void Bind()
        {
            if (_button != null) return;

            var canvas = GetComponent<Canvas>() ?? GetComponentInParent<Canvas>();
            Transform root = null;
            if (canvas != null)
            {
                foreach (var t in canvas.GetComponentsInChildren<Transform>(true))
                {
                    if (t.name == "OverviewButton")
                    {
                        root = t;
                        break;
                    }
                }
            }

            if (root == null) return;

            _button = root.GetComponent<Button>();
            _label  = root.GetComponentInChildren<TextMeshProUGUI>(true);
            if (_button != null)
            {
                _button.onClick.RemoveListener(OnToggleClicked);
                _button.onClick.AddListener(OnToggleClicked);
                var cam = BoardCameraController.Instance;
                if (cam != null) RefreshLabel(cam.IsOverviewMode);
            }
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
