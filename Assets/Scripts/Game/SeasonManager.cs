using System;
using UnityEngine;
using Sugoroku.Data;

namespace Sugoroku.Game
{
    /// <summary>
    /// 3大ステージ（研究執筆・学会・バイトシーズン）の進行管理およびルール補正エンジン。
    /// </summary>
    public class SeasonManager : MonoBehaviour
    {
        public static SeasonManager Instance { get; private set; }

        public static SeasonType CurrentSeasonType
        {
            get => GameSession.CurrentSeason;
            set => GameSession.CurrentSeason = value;
        }

        public static SeasonConfig CurrentConfig => SeasonConfig.Get(CurrentSeasonType);

        public static event Action<SeasonConfig> OnSeasonChanged;
        public static event Action<SeasonConfig> OnSeasonStageCleared;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        public static void ResetToFirstSeason()
        {
            CurrentSeasonType = SeasonType.ResearchWriting;
            OnSeasonChanged?.Invoke(CurrentConfig);
        }

        /// <summary>
        /// 次のシーズンステージへ進む。
        /// </summary>
        /// <returns>最終ステージをクリアしてゲーム修了なら true、次のステージへ進んだなら false</returns>
        public static bool AdvanceToNextSeason()
        {
            var oldConfig = CurrentConfig;
            OnSeasonStageCleared?.Invoke(oldConfig);

            switch (CurrentSeasonType)
            {
                case SeasonType.ResearchWriting:
                    CurrentSeasonType = SeasonType.Conference;
                    OnSeasonChanged?.Invoke(CurrentConfig);
                    return false;

                case SeasonType.Conference:
                    CurrentSeasonType = SeasonType.PartTime;
                    OnSeasonChanged?.Invoke(CurrentConfig);
                    return false;

                case SeasonType.PartTime:
                default:
                    // 最終ステージクリア！大学院修了！
                    return true;
            }
        }

        // --- ルール補正計算ヘルパー ---

        public static int ModifyIfGain(int rawGain) => CurrentConfig.ModifyIfGain(rawGain);

        public static int ModifyMentalDrain(int rawDrain) => CurrentConfig.ModifyMentalDrain(rawDrain);

        public static int ModifyMentalRest(int rawGain) => CurrentConfig.ModifyMentalRest(rawGain);

        public static int ModifyMoneyCost(int rawCost) => CurrentConfig.ModifyMoneyCost(rawCost);

        public static int ModifyMoneyGain(int rawGain) => CurrentConfig.ModifyMoneyGain(rawGain);

        public static int ModifyVirtueGain(int rawGain) => CurrentConfig.ModifyVirtueGain(rawGain);
    }
}
