using System;
using UnityEngine;

namespace Sugoroku.Data
{
    /// <summary>
    /// 各シーズン（ステージ）の設定情報およびルール補正定義。
    /// </summary>
    [Serializable]
    public class SeasonConfig
    {
        public SeasonType Type;
        public int StageNumber;
        public string Title;
        public string Subtitle;
        public string Description;
        public string GoalTitle;
        public Color ThemeColor;
        public int DefaultCellCount;

        // ルール補正係数
        public float IfGainMultiplier = 1.0f;
        public int IfFlatBonus = 0;
        public float MentalDrainMultiplier = 1.0f;
        public float MentalRestMultiplier = 1.0f;
        public float MoneyCostMultiplier = 1.0f;
        public float MoneyGainMultiplier = 1.0f;
        public float VirtueGainMultiplier = 1.0f;

        // イベントタグ絞り込み
        public string[] PreferredTags;

        public static SeasonConfig Get(SeasonType type) => type switch
        {
            SeasonType.ResearchWriting => new SeasonConfig
            {
                Type = SeasonType.ResearchWriting,
                StageNumber = 1,
                Title = "研究執筆シーズン",
                Subtitle = "Stage 1: Research & Writing",
                Description = "【論文執筆ブースト】IF獲得量+50%！ただし徹夜と締め切りでメンタル消費+20%",
                GoalTitle = "論文ドラフト提出",
                ThemeColor = new Color(0.2f, 0.65f, 1f, 1f),
                DefaultCellCount = 14,
                IfGainMultiplier = 1.5f,
                IfFlatBonus = 0,
                MentalDrainMultiplier = 1.2f,
                MentalRestMultiplier = 1.0f,
                MoneyCostMultiplier = 1.0f,
                MoneyGainMultiplier = 1.0f,
                VirtueGainMultiplier = 1.0f,
                PreferredTags = new[] { "研究", "査読", "ゼミ", "教授", "講義" }
            },
            SeasonType.Conference => new SeasonConfig
            {
                Type = SeasonType.Conference,
                StageNumber = 2,
                Title = "学会シーズン",
                Subtitle = "Stage 2: Academic Conference",
                Description = "【ハイリスク・ハイリターン】所持金消費+50%（旅費・参加費）だが、IF獲得時+10ptボーナス＆徳2倍！",
                GoalTitle = "学会発表・総括",
                ThemeColor = new Color(0.85f, 0.45f, 1f, 1f),
                DefaultCellCount = 14,
                IfGainMultiplier = 1.0f,
                IfFlatBonus = 10,
                MentalDrainMultiplier = 1.0f,
                MentalRestMultiplier = 1.0f,
                MoneyCostMultiplier = 1.5f,
                MoneyGainMultiplier = 1.0f,
                VirtueGainMultiplier = 2.0f,
                PreferredTags = new[] { "学会", "研究費", "後輩", "緊急" }
            },
            SeasonType.PartTime => new SeasonConfig
            {
                Type = SeasonType.PartTime,
                StageNumber = 3,
                Title = "バイトシーズン",
                Subtitle = "Stage 3: Part-Time Job & Graduation",
                Description = "【生活費稼ぎ＆息抜き】所持金獲得量+50%！休憩のメンタル回復+50%！学費を完納し修了を目指せ！",
                GoalTitle = "学位審査・修了判定",
                ThemeColor = new Color(1f, 0.65f, 0.15f, 1f),
                DefaultCellCount = 14,
                IfGainMultiplier = 1.0f,
                IfFlatBonus = 0,
                MentalDrainMultiplier = 1.0f,
                MentalRestMultiplier = 1.5f,
                MoneyCostMultiplier = 1.0f,
                MoneyGainMultiplier = 1.5f,
                VirtueGainMultiplier = 1.0f,
                PreferredTags = new[] { "バイト", "生活", "就活", "事務" }
            },
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };

        public int ModifyIfGain(int rawGain)
        {
            if (rawGain <= 0) return rawGain;
            int scaled = Mathf.RoundToInt(rawGain * IfGainMultiplier) + IfFlatBonus;
            return Mathf.Max(rawGain, scaled);
        }

        public int ModifyMentalDrain(int rawDrain)
        {
            if (rawDrain >= 0) return rawDrain;
            return Mathf.RoundToInt(rawDrain * MentalDrainMultiplier);
        }

        public int ModifyMentalRest(int rawGain)
        {
            if (rawGain <= 0) return rawGain;
            return Mathf.RoundToInt(rawGain * MentalRestMultiplier);
        }

        public int ModifyMoneyCost(int rawCost)
        {
            if (rawCost >= 0) return rawCost;
            return Mathf.RoundToInt(rawCost * MoneyCostMultiplier);
        }

        public int ModifyMoneyGain(int rawGain)
        {
            if (rawGain <= 0) return rawGain;
            return Mathf.RoundToInt(rawGain * MoneyGainMultiplier);
        }

        public int ModifyVirtueGain(int rawGain)
        {
            if (rawGain <= 0) return rawGain;
            return Mathf.RoundToInt(rawGain * VirtueGainMultiplier);
        }
    }
}
