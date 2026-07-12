using System.Collections;
using UnityEngine;
using Sugoroku.Visual;

namespace Sugoroku.Audio
{
    /// <summary>
    /// BGM/SE管理。Kenney Interface Sounds / BoardgamePack Audio を使用。
    /// 各AudioClipはInspectorまたはAwakeでResources.Loadして設定する。
    /// BGMは2本のAudioSourceを切り替えてクロスフェードする。
    /// </summary>
    public class GameAudioController : MonoBehaviour
    {
        public static GameAudioController Instance { get; private set; }

        [Header("BGM（未設定なら無音でスキップ）")]
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

        [Header("超レアイベント演出（未設定なら無音でスキップ）")]
        public AudioClip RareFanfareSe;

        private const float BgmCrossfadeSeconds = 0.8f;

        private AudioSource _bgmSourceA;
        private AudioSource _bgmSourceB;
        private AudioSource _activeBgmSource;
        private AudioSource _seSource;
        private Coroutine   _crossfadeRoutine;
        private float _bgmVolume = 0.7f;
        private float _seVolume  = 1.0f;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            _bgmSourceA = gameObject.AddComponent<AudioSource>();
            _bgmSourceA.loop = true;
            _bgmSourceA.volume = 0f;

            _bgmSourceB = gameObject.AddComponent<AudioSource>();
            _bgmSourceB.loop = true;
            _bgmSourceB.volume = 0f;

            _activeBgmSource = _bgmSourceA;

            _seSource = gameObject.AddComponent<AudioSource>();
            _seSource.volume = 1f;

            _bgmVolume = Sugoroku.Data.GameSession.BgmVolume;
            _seVolume  = Sugoroku.Data.GameSession.SeVolume;

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

        /// <summary>クロスフェードでBGMを切り替える。同じクリップが再生中なら何もしない。</summary>
        public void PlayBgm(AudioClip clip)
        {
            if (clip == null || _activeBgmSource.clip == clip) return;

            var next = _activeBgmSource == _bgmSourceA ? _bgmSourceB : _bgmSourceA;
            next.clip = clip;
            next.volume = 0f;
            next.Play();

            if (_crossfadeRoutine != null) StopCoroutine(_crossfadeRoutine);
            _crossfadeRoutine = StartCoroutine(CrossfadeTo(next));
        }

        private IEnumerator CrossfadeTo(AudioSource next)
        {
            var prev = _activeBgmSource;
            _activeBgmSource = next;

            float t = 0f;
            while (t < BgmCrossfadeSeconds)
            {
                t += Time.unscaledDeltaTime;
                float ratio = Mathf.Clamp01(t / BgmCrossfadeSeconds);
                next.volume = _bgmVolume * ratio;
                if (prev != next) prev.volume = _bgmVolume * (1f - ratio);
                yield return null;
            }

            next.volume = _bgmVolume;
            if (prev != next)
            {
                prev.volume = 0f;
                prev.Stop();
            }
            _crossfadeRoutine = null;
        }

        public void PlayTitleBgm()  => PlayBgm(TitleBgm);
        public void PlayGameBgm()   => PlayBgm(GameBgm);
        public void PlayResultBgm() => PlayBgm(ResultBgm);

        public void StopBgm()
        {
            if (_crossfadeRoutine != null) { StopCoroutine(_crossfadeRoutine); _crossfadeRoutine = null; }
            _bgmSourceA?.Stop();
            _bgmSourceB?.Stop();
        }

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

        /// <summary>超レアイベント発生時のファンファーレ。未設定時は無音。</summary>
        public void PlayRareEventFanfare() => PlaySe(RareFanfareSe);

        public void SetBgmVolume(float v)
        {
            _bgmVolume = v;
            if (_activeBgmSource != null) _activeBgmSource.volume = v;
        }

        public void SetSeVolume(float v) => _seVolume = v;
    }
}
