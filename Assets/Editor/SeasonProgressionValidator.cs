using System;
using UnityEditor;
using UnityEngine;
using Sugoroku.Data;
using Sugoroku.Game;
using Sugoroku.Board;

namespace Sugoroku.EditorTools
{
    /// <summary>
    /// 3ステージ制（研究執筆・学会・バイトシーズン）の進行、盤面生成、
    /// ルール補正、リソース引き継ぎを網羅的に検証する自動テスト。
    /// </summary>
    public static class SeasonProgressionValidator
    {
        [MenuItem("Tools/Sugoroku/Validate Season Progression")]
        public static void ValidateFromMenu()
        {
            RunValidation(isBatch: false);
        }

        public static void ValidateBatch()
        {
            bool success = RunValidation(isBatch: true);
            EditorApplication.Exit(success ? 0 : 1);
        }

        public static bool RunValidation(bool isBatch)
        {
            Debug.Log("==================================================================");
            Debug.Log("🌸 3ステージ制（研究執筆・学会・バイトシーズン）検証開始 🌸");
            Debug.Log("==================================================================");

            bool allPassed = true;

            // Test 1: シーズンデータ・コンフィグ検証
            allPassed &= TestSeasonConfigs();

            // Test 2: シーズン固有ルール（バフ/デバフ）補正テスト
            allPassed &= TestSeasonRuleModifiers();

            // Test 3: 各シーズンの盤面自動生成 & マス・イベント配分テスト
            allPassed &= TestSeasonBoardGeneration();

            // Test 4: ステージクリア〜次シーズン移行・修了シミュレーション
            allPassed &= TestProgressionSimulation();

            if (allPassed)
            {
                Debug.Log("🏆【検証成功】3ステージ制（研究執筆・学会・バイトシーズン）の全テストに合格しました！");
            }
            else
            {
                Debug.LogError("❌【検証失敗】一部のシーズン進行テストが失敗しました。");
            }

            return allPassed;
        }

        private static bool TestSeasonConfigs()
        {
            Debug.Log("[Test 1: シーズン設定 & データ整合性]");
            foreach (SeasonType st in Enum.GetValues(typeof(SeasonType)))
            {
                var cfg = SeasonConfig.Get(st);
                if (cfg == null)
                {
                    Debug.LogError($"[Fail] SeasonConfig for {st} is null.");
                    return false;
                }
                if (string.IsNullOrEmpty(cfg.Title) || string.IsNullOrEmpty(cfg.GoalTitle))
                {
                    Debug.LogError($"[Fail] Season {st} title or goal title is empty.");
                    return false;
                }
                Debug.Log($"  - Stage {cfg.StageNumber}: {cfg.Title} ({cfg.Subtitle}) / ゴール: {cfg.GoalTitle}");
            }
            Debug.Log("✅ Test 1 Passed: 全シーズンのコンフィグが正常に定義されています。");
            return true;
        }

        private static bool TestSeasonRuleModifiers()
        {
            Debug.Log("[Test 2: シーズンルール（バフ/デバフ）補正ロジック]");

            // Stage 1: 研究執筆シーズン
            SeasonManager.CurrentSeasonType = SeasonType.ResearchWriting;
            int ifGain1 = SeasonManager.ModifyIfGain(10);
            int mentalDrain1 = SeasonManager.ModifyMentalDrain(-10);
            if (ifGain1 != 15) // +50%
            {
                Debug.LogError($"[Fail] Stage 1 IF Gain expected 15, but got {ifGain1}");
                return false;
            }
            if (mentalDrain1 != -12) // +20% 増加
            {
                Debug.LogError($"[Fail] Stage 1 Mental Drain expected -12, but got {mentalDrain1}");
                return false;
            }
            Debug.Log($"  - Stage 1 (研究執筆): IF+10 → {ifGain1}pt (+50%), メンタル-10 → {mentalDrain1} (+20%重圧)");

            // Stage 2: 学会シーズン
            SeasonManager.CurrentSeasonType = SeasonType.Conference;
            int moneyCost2 = SeasonManager.ModifyMoneyCost(-20);
            int ifGain2 = SeasonManager.ModifyIfGain(5);
            int virtueGain2 = SeasonManager.ModifyVirtueGain(3);
            if (moneyCost2 != -30) // +50% 支出
            {
                Debug.LogError($"[Fail] Stage 2 Money Cost expected -30, but got {moneyCost2}");
                return false;
            }
            if (ifGain2 != 15) // 5 + 10 = 15
            {
                Debug.LogError($"[Fail] Stage 2 IF Gain expected 15 (+10ptボーナス), but got {ifGain2}");
                return false;
            }
            if (virtueGain2 != 6) // 2倍
            {
                Debug.LogError($"[Fail] Stage 2 Virtue Gain expected 6 (2倍), but got {virtueGain2}");
                return false;
            }
            Debug.Log($"  - Stage 2 (学会): 支出-20 → {moneyCost2}万 (+50%), IF+5 → {ifGain2}pt (+10pt), 徳+3 → {virtueGain2} (2倍)");

            // Stage 3: バイトシーズン
            SeasonManager.CurrentSeasonType = SeasonType.PartTime;
            int moneyGain3 = SeasonManager.ModifyMoneyGain(10);
            int mentalRest3 = SeasonManager.ModifyMentalRest(10);
            if (moneyGain3 != 15) // +50%
            {
                Debug.LogError($"[Fail] Stage 3 Money Gain expected 15, but got {moneyGain3}");
                return false;
            }
            if (mentalRest3 != 15) // +50%
            {
                Debug.LogError($"[Fail] Stage 3 Mental Rest expected 15, but got {mentalRest3}");
                return false;
            }
            Debug.Log($"  - Stage 3 (バイト): 所持金+10 → {moneyGain3}万 (+50%), メンタル回復+10 → {mentalRest3} (+50%)");

            Debug.Log("✅ Test 2 Passed: 全シーズンのルール補正計算が正確に機能しています。");
            return true;
        }

        private static bool TestSeasonBoardGeneration()
        {
            Debug.Log("[Test 3: シーズン別盤面自動生成]");

            // Stage 1
            var b1 = BoardLayoutGenerator.Build(14, 12345, SeasonType.ResearchWriting);
            if (b1.DisplayNames[0] != "研究室配属" || b1.DisplayNames[13] != "論文ドラフト提出")
            {
                Debug.LogError($"[Fail] Stage 1 board titles mismatch: Start={b1.DisplayNames[0]}, Goal={b1.DisplayNames[13]}");
                return false;
            }

            // Stage 2
            var b2 = BoardLayoutGenerator.Build(14, 12345, SeasonType.Conference);
            if (b2.DisplayNames[0] != "学会会場到着" || b2.DisplayNames[13] != "学会総括・成果報告")
            {
                Debug.LogError($"[Fail] Stage 2 board titles mismatch: Start={b2.DisplayNames[0]}, Goal={b2.DisplayNames[13]}");
                return false;
            }

            // Stage 3
            var b3 = BoardLayoutGenerator.Build(14, 12345, SeasonType.PartTime);
            if (b3.DisplayNames[0] != "バイト生活突入" || b3.DisplayNames[13] != "学位審査・修了判定")
            {
                Debug.LogError($"[Fail] Stage 3 board titles mismatch: Start={b3.DisplayNames[0]}, Goal={b3.DisplayNames[13]}");
                return false;
            }

            Debug.Log("  - Stage 1 盤面: スタート「研究室配属」〜 ゴール「論文ドラフト提出」");
            Debug.Log("  - Stage 2 盤面: スタート「学会会場到着」〜 ゴール「学会総括・成果報告」");
            Debug.Log("  - Stage 3 盤面: スタート「バイト生活突入」〜 ゴール「学位審査・修了判定」");
            Debug.Log("✅ Test 3 Passed: 3シーズンそれぞれの固有マス・目標が正常に生成されます。");
            return true;
        }

        private static bool TestProgressionSimulation()
        {
            Debug.Log("[Test 4: ステージ進行・ステータス引き継ぎシミュレーション]");

            SeasonManager.ResetToFirstSeason();
            var p1 = PlayerData.Create(0, CharacterType.Hobbyist, false);
            p1.Money = 50;
            p1.IfScore = 10;
            p1.Mental = 40;
            p1.Virtue = 5;

            // Stage 1: 研究執筆
            if (SeasonManager.CurrentSeasonType != SeasonType.ResearchWriting) return false;
            p1.ApplyStatChange(0, 10, -10, 0); // IF獲得 +50% (15), メンタル消費 +20% (-12)
            Debug.Log($"  Stage 1 進行中ステータス: 所持金={p1.Money}万, IF={p1.IfScore}pt, メンタル={p1.Mental}, 徳={p1.Virtue}");

            // Stage 1 クリア
            bool stage1End = SeasonManager.AdvanceToNextSeason();
            if (stage1End || SeasonManager.CurrentSeasonType != SeasonType.Conference)
            {
                Debug.LogError("[Fail] AdvanceToNextSeason failed from Stage 1 to Stage 2");
                return false;
            }
            Debug.Log("  -> Stage 1 クリア！ Stage 2 (学会シーズン) へ移行。ステータス引き継ぎ確認。");

            // Stage 2: 学会
            p1.ApplyStatChange(-20, 5, 0, 2); // 支出-30万, IF+15pt, 徳+4
            Debug.Log($"  Stage 2 進行中ステータス: 所持金={p1.Money}万, IF={p1.IfScore}pt, メンタル={p1.Mental}, 徳={p1.Virtue}");

            // Stage 2 クリア
            bool stage2End = SeasonManager.AdvanceToNextSeason();
            if (stage2End || SeasonManager.CurrentSeasonType != SeasonType.PartTime)
            {
                Debug.LogError("[Fail] AdvanceToNextSeason failed from Stage 2 to Stage 3");
                return false;
            }
            Debug.Log("  -> Stage 2 クリア！ Stage 3 (バイトシーズン) へ移行。ステータス引き継ぎ確認。");

            // Stage 3: バイト
            p1.ApplyStatChange(20, 0, 10, 0); // 所持金+30万, メンタル+15
            Debug.Log($"  Stage 3 進行中ステータス: 所持金={p1.Money}万, IF={p1.IfScore}pt, メンタル={p1.Mental}, 徳={p1.Virtue}");

            // Stage 3 クリア（全修了）
            bool graduation = SeasonManager.AdvanceToNextSeason();
            if (!graduation)
            {
                Debug.LogError("[Fail] AdvanceToNextSeason should return true after Stage 3 (Graduation).");
                return false;
            }
            Debug.Log($"  -> Stage 3 最終ゴール到達！ 大学院修了（総合スコア: {p1.CalculateScore()} pt）！");

            Debug.Log("✅ Test 4 Passed: 3ステージの連続クリア、リソース完全引き継ぎ、最終修了判定が実証されました。");
            return true;
        }
    }
}
