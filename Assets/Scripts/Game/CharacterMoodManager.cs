using System;
using System.Collections.Generic;
using UnityEngine;
using Sugoroku.Data;
using Sugoroku.Visual;

namespace Sugoroku.Game
{
    /// <summary>
    /// 各プレイヤーの表情状態（笑顔:3, 涙目:1, 真顔:2）をリアルタイムに管理する。
    /// 
    /// 仕様:
    /// - メンタル回復などパラメータ上昇時: 4秒間笑顔 (3)
    /// - メンタル50%以下 (永久) もしくは メンタル下落時: 4秒間涙目 (1)
    /// - それ以外の時: 真顔 (2)
    /// </summary>
    [DisallowMultipleComponent]
    public class CharacterMoodManager : MonoBehaviour
    {
        public static CharacterMoodManager Instance { get; private set; }

        public const float EmotionDuration = 4.0f;

        public static event Action<PlayerData, CharacterMood> OnMoodChanged;

        private class PlayerMoodState
        {
            public float SmileTimer;
            public float CryingTimer;
            public CharacterMood CurrentMood = CharacterMood.Neutral;
        }

        private readonly Dictionary<int, PlayerMoodState> _playerStates = new();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;
        }

        public void Initialize()
        {
            Instance = this;
            StatChangeNotifier.OnChanged -= HandleStatChanged;
            StatChangeNotifier.OnChanged += HandleStatChanged;
        }

        private void OnEnable()
        {
            Initialize();
        }

        private void OnDisable()
        {
            StatChangeNotifier.OnChanged -= HandleStatChanged;
            if (Instance == this)
                Instance = null;
        }

        private void Update()
        {
            if (_playerStates.Count == 0) return;

            float dt = Time.deltaTime;
            var gm = GameManager.Instance;
            if (gm == null || !gm.IsInitialized) return;

            var players = gm.GetAllPlayers();
            foreach (var p in players)
            {
                if (p == null) continue;
                if (!_playerStates.TryGetValue(p.Index, out var state))
                    continue;

                bool timerUpdated = false;
                if (state.SmileTimer > 0f)
                {
                    state.SmileTimer = Mathf.Max(0f, state.SmileTimer - dt);
                    timerUpdated = true;
                }
                if (state.CryingTimer > 0f)
                {
                    state.CryingTimer = Mathf.Max(0f, state.CryingTimer - dt);
                    timerUpdated = true;
                }

                if (timerUpdated)
                {
                    CheckMoodTransition(p, state);
                }
            }
        }

        private void HandleStatChanged(PlayerData player, int deltaMoney, int deltaIf, int deltaMental, int deltaVirtue)
        {
            if (player == null) return;
            var state = GetOrCreateState(player.Index);

            // メンタル下落時: 4秒くらい涙目（他ステータスが同時に上がっても涙目優先）
            if (deltaMental < 0)
            {
                state.CryingTimer = EmotionDuration;
                state.SmileTimer = 0f; // 笑顔をキャンセルして涙目に
            }
            // メンタル回復時（deltaMental > 0）: 涙目を解除して4秒笑顔
            else if (deltaMental > 0)
            {
                bool isLowMental = player.MaxMental > 0 && player.Mental <= (player.MaxMental * 0.5f);
                if (!isLowMental)
                {
                    state.SmileTimer = EmotionDuration;
                    state.CryingTimer = 0f;
                }
            }
            // その他パラメータ上昇時: 4秒くらい笑顔
            // ※仕様: メンタルが減った場合（CryingTimer中）やメンタル50%以下の場合はその他ステータスが上がっても涙目を維持
            else if (deltaMoney > 0 || deltaIf > 0 || deltaVirtue > 0)
            {
                bool isLowMental = player.MaxMental > 0 && player.Mental <= (player.MaxMental * 0.5f);
                if (!isLowMental && state.CryingTimer <= 0f)
                {
                    state.SmileTimer = EmotionDuration;
                }
            }

            CheckMoodTransition(player, state);
        }

        private void CheckMoodTransition(PlayerData player, PlayerMoodState state)
        {
            var newMood = EvaluateMood(player, state);
            if (newMood != state.CurrentMood)
            {
                state.CurrentMood = newMood;
                OnMoodChanged?.Invoke(player, newMood);
            }
        }

        private PlayerMoodState GetOrCreateState(int playerIndex)
        {
            if (!_playerStates.TryGetValue(playerIndex, out var state))
            {
                state = new PlayerMoodState();
                _playerStates[playerIndex] = state;
            }
            return state;
        }

        public static CharacterMood GetMood(PlayerData player)
        {
            if (player == null) return CharacterMood.Neutral;

            if (Instance != null && Instance._playerStates.TryGetValue(player.Index, out var state))
            {
                return EvaluateMood(player, state);
            }

            // Instance がない場合の静的評価
            bool isLowMental = player.MaxMental > 0 && player.Mental <= (player.MaxMental * 0.5f);
            return isLowMental ? CharacterMood.Crying : CharacterMood.Neutral;
        }

        private static CharacterMood EvaluateMood(PlayerData player, PlayerMoodState state)
        {
            if (player == null) return CharacterMood.Neutral;

            // 1. メンタル50%以下(の間は永久) もしくは メンタル下落時に4秒くらい涙目(各pngの1)
            // ※仕様: メンタルが減ったらその他ステータスが上がっても涙目になる
            bool isLowMental = player.MaxMental > 0 && player.Mental <= (player.MaxMental * 0.5f);
            if (isLowMental || state.CryingTimer > 0f)
                return CharacterMood.Crying;

            // 2. パラメータ上昇時では4秒くらい笑顔(各pngの3)
            if (state.SmileTimer > 0f)
                return CharacterMood.Smile;

            // 3. それ以外の時は真顔(各pngの2)
            return CharacterMood.Neutral;
        }

        public static void ResetPlayerState(int playerIndex)
        {
            if (Instance != null && Instance._playerStates.TryGetValue(playerIndex, out var state))
            {
                state.SmileTimer = 0f;
                state.CryingTimer = 0f;
                state.CurrentMood = CharacterMood.Neutral;
            }
        }
    }
}
