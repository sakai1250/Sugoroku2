using UnityEngine;
using Sugoroku.Visual;

namespace Sugoroku.Audio
{
    /// <summary>
    /// BGM/SE管理。Kenney Interface Sounds / BoardgamePack Audio を使用。
    /// 各AudioClipはInspectorまたはAwakeでResources.Loadして設定する。
    /// </summary>
    public class GameAudioController : MonoBehaviour
    {
        public static GameAudioController Instance { get; private set; }

        [Header("BGM")]
        public AudioClip TitleBgm;
        public AudioClip GameBgm;
        public AudioClip ResultBgm;

        [Header("SE (Kenney Interface Sounds / BoardgamePack)")]
        public AudioClip DiceRollSe;      // BoardgamePack/Audio/dieThrow1
        public AudioClip ButtonClickSe;   // InterfaceSounds/click_001
        public AudioClip ConfirmSe;       // InterfaceSounds/confirmation_001
        public AudioClip ErrorSe;         // InterfaceSounds/error_001
        public AudioClip GoalSe;          // confirmation_001 流用
        public AudioClip GameOverSe;      // error_001 流用

        private AudioSource _bgmSource;
        private AudioSource _seSource;
        private float _bgmVolume = 0.7f;
        private float _seVolume  = 1.0f;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            _bgmSource       = gameObject.AddComponent<AudioSource>();
            _bgmSource.loop  = true;
            _bgmSource.volume = _bgmVolume;

            _seSource         = gameObject.AddComponent<AudioSource>();
            _seSource.volume  = _seVolume;

            LoadKenneyClips();
        }

        private void LoadKenneyClips()
        {
            // Kenney クリップが Inspector 未設定なら Resources から自動ロード
            if (DiceRollSe    == null) DiceRollSe    = KenneyAssets.LoadAudio(KenneyAssets.BoardgamePack.AudioDieThrow);
            if (ButtonClickSe == null) ButtonClickSe = KenneyAssets.LoadAudio(KenneyAssets.InterfaceSounds.Click);
            if (ConfirmSe     == null) ConfirmSe     = KenneyAssets.LoadAudio(KenneyAssets.InterfaceSounds.Confirm);
            if (ErrorSe       == null) ErrorSe       = KenneyAssets.LoadAudio(KenneyAssets.InterfaceSounds.Error);
            if (GoalSe        == null) GoalSe        = ConfirmSe;
            if (GameOverSe    == null) GameOverSe    = ErrorSe;
        }

        public void PlayBgm(AudioClip clip)
        {
            if (clip == null || _bgmSource.clip == clip) return;
            _bgmSource.clip = clip;
            _bgmSource.Play();
        }

        public void PlayTitleBgm()  => PlayBgm(TitleBgm);
        public void PlayGameBgm()   => PlayBgm(GameBgm);
        public void PlayResultBgm() => PlayBgm(ResultBgm);
        public void StopBgm()       => _bgmSource?.Stop();

        public void PlaySe(AudioClip clip)
        {
            if (clip == null) return;
            _seSource.pitch = 1f;
            _seSource.PlayOneShot(clip, _seVolume);
        }

        public void PlaySeWithPitch(AudioClip clip, float pitch, float volumeScale = 1f)
        {
            if (clip == null || _seSource == null) return;
            _seSource.pitch = pitch;
            _seSource.PlayOneShot(clip, _seVolume * volumeScale);
            _seSource.pitch = 1f;
        }

        public void PlayDiceRoll()    => PlaySe(DiceRollSe);
        public void PlayDiceLand()    => PlaySe(ConfirmSe);
        public void PlayButtonClick() => PlaySe(ButtonClickSe);
        public void PlayConfirm()     => PlaySe(ConfirmSe);

        /// <summary>キャラ選択の切り替え音（レトロなカチッ）。</summary>
        public void PlayRetroSelect() => PlaySeWithPitch(ButtonClickSe, 1.18f, 0.95f);

        /// <summary>進路決定 — ドアが閉まるような低めの SE。</summary>
        public void PlayDoorClose() => PlaySeWithPitch(ConfirmSe, 0.52f, 1.05f);
        public void PlayError()       => PlaySe(ErrorSe);
        public void PlayGoal()        => PlaySe(GoalSe);
        public void PlayGameOver()    => PlaySe(GameOverSe);

        public void SetBgmVolume(float v) { _bgmVolume = v; if (_bgmSource) _bgmSource.volume = v; }
        public void SetSeVolume(float v)  { _seVolume  = v; if (_seSource)  _seSource.volume  = v; }
    }
}
