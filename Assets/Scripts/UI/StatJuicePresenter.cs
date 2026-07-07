using UnityEngine;
using Sugoroku.Data;
using Sugoroku.Game;

namespace Sugoroku.UI
{
    /// <summary>ステータス変更を HUD 発光・駒上フロート表示へ接続（requirements §2.2）。</summary>
    public class StatJuicePresenter : MonoBehaviour
    {
        [SerializeField] private GameHUD       _hud;
        [SerializeField] private HudStatFlash  _hudFlash;
        [SerializeField] private FloatingTextUI _floating;

        private void Awake()
        {
            _hud       ??= GetComponentInParent<GameHUD>();
            _hudFlash  ??= GetComponent<HudStatFlash>();
            _floating  ??= FloatingTextUI.Instance ?? FindFirstObjectByType<FloatingTextUI>();
            if (_hudFlash == null)
                _hudFlash = gameObject.AddComponent<HudStatFlash>();
        }

        private void OnEnable()  => StatChangeNotifier.OnChanged += HandleStatChanged;
        private void OnDisable() => StatChangeNotifier.OnChanged -= HandleStatChanged;

        private void HandleStatChanged(PlayerData player, int money, int ifScore, int mental, int virtue)
        {
            if (player == null) return;

            var gm = GameManager.Instance;
            Vector3 worldPos = Vector3.zero;
            if (gm != null)
            {
                var piece = gm.GetPiece(player.Index);
                if (piece != null) worldPos = piece.transform.position;
            }

            _floating?.ShowStatChange(money, ifScore, mental, virtue, worldPos);

            if (gm != null && gm.GetCurrentPlayer() == player)
            {
                _hud?.RefreshAll();
                _hud?.AnimateStatChange(player, money, ifScore, mental, virtue);
                _hudFlash?.Flash(money, ifScore, mental, virtue);
            }
        }
    }
}
