using UnityEngine;
using UnityEngine.EventSystems;

namespace Sugoroku.UI
{
    /// <summary>カードホバー時およびキーボードフォーカス時の §2.4 の軽いフィードバック。</summary>
    public class CharacterSelectCardHover : MonoBehaviour, IPointerEnterHandler, ISelectHandler
    {
        private CharacterSelectController _host;
        private int _index;

        public void Setup(CharacterSelectController host, int index)
        {
            _host  = host;
            _index = index;
        }

        public void OnPointerEnter(PointerEventData eventData) =>
            _host?.OnCardHover(_index);

        // キーボード/ゲームパッドのフォーカス移動でもマウスホバーと同等のプレビューを行う（WCAG 2.1.1）
        public void OnSelect(BaseEventData eventData) =>
            _host?.OnCardHover(_index);
    }
}
