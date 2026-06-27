using UnityEngine;

namespace Sugoroku.Data
{
    /// <summary>
    /// events.json 全選択肢のうち、1回のイベントで破産・失踪になり得る閾値。
    /// 所持金・メンタルがこの値以下なら「ピンチ！！」表示（現状: 最大 -40 / -40）。
    /// </summary>
    public static class StatPinchThresholds
    {
        const int FallbackWorstMoneyLoss  = GameConfig.PinchMoneyThresholdFallback;
        const int FallbackWorstMentalLoss = GameConfig.PinchMentalThresholdFallback;

        static bool _computed;
        static int  _worstMoneyLoss;
        static int  _worstMentalLoss;

        /// <summary>任意イベント1回の最大所持金減少（正の値）。</summary>
        public static int WorstEventMoneyLoss
        {
            get { EnsureComputed(); return _worstMoneyLoss; }
        }

        /// <summary>任意イベント1回の最大メンタル減少（正の値）。</summary>
        public static int WorstEventMentalLoss
        {
            get { EnsureComputed(); return _worstMentalLoss; }
        }

        public static bool IsMoneyPinch(int money) => money <= WorstEventMoneyLoss;

        public static bool IsMentalPinch(int mental) => mental <= WorstEventMentalLoss;

        static void EnsureComputed()
        {
            if (_computed) return;

            _worstMoneyLoss  = FallbackWorstMoneyLoss;
            _worstMentalLoss = FallbackWorstMentalLoss;

            var textAsset = Resources.Load<TextAsset>("EventMasters/events");
            if (textAsset != null)
            {
                string json = "{\"Events\":" + textAsset.text + "}";
                var wrapper = JsonUtility.FromJson<EventMasterList>(json);
                if (wrapper?.Events != null)
                {
                    foreach (var ev in wrapper.Events)
                    {
                        if (ev?.Choices == null) continue;
                        foreach (var choice in ev.Choices)
                        {
                            if (choice == null) continue;
                            if (choice.MoneyChange < 0)
                                _worstMoneyLoss = Mathf.Max(_worstMoneyLoss, -choice.MoneyChange);
                            if (choice.MentalChange < 0)
                                _worstMentalLoss = Mathf.Max(_worstMentalLoss, -choice.MentalChange);
                        }
                    }
                }
            }

            _computed = true;
        }
    }
}
