using NUnit.Framework;
using UnityEngine;
using Sugoroku.Data;
using Sugoroku.Game;
using Sugoroku.Visual;

namespace Sugoroku.Tests
{
    [TestFixture]
    public class CharacterMoodTests
    {
        private GameObject _holder;
        private CharacterMoodManager _manager;

        [SetUp]
        public void SetUp()
        {
            _holder = new GameObject("TestMoodManager");
            _manager = _holder.AddComponent<CharacterMoodManager>();
            _manager.Initialize();
        }

        [TearDown]
        public void TearDown()
        {
            if (_holder != null)
                Object.DestroyImmediate(_holder);
        }

        [Test]
        public void InitialMood_WithFullMental_IsNeutral()
        {
            var player = PlayerData.Create(0, CharacterType.Hobbyist, false);
            player.MaxMental = 50;
            player.Mental = 50;

            var mood = CharacterMoodManager.GetMood(player);
            Assert.AreEqual(CharacterMood.Neutral, mood, "メンタル50%超かつ変化なしの場合は真顔(2)");
        }

        [Test]
        public void LowMental_Below50Percent_IsCrying()
        {
            var player = PlayerData.Create(0, CharacterType.Hobbyist, false);
            player.MaxMental = 50;
            player.Mental = 25; // 50%ちょうど

            var mood = CharacterMoodManager.GetMood(player);
            Assert.AreEqual(CharacterMood.Crying, mood, "メンタル50%以下の場合は涙目(1)");

            player.Mental = 10;
            mood = CharacterMoodManager.GetMood(player);
            Assert.AreEqual(CharacterMood.Crying, mood, "メンタル50%未満の場合も涙目(1)");
        }

        [Test]
        public void StatIncrease_TriggersSmile()
        {
            var player = PlayerData.Create(0, CharacterType.Serious, false);
            player.MaxMental = 50;
            player.Mental = 40;

            // パラメータ上昇（メンタル回復）
            player.ApplyStatChange(0, 0, 5, 0, applyCharacterPassives: false);

            var mood = CharacterMoodManager.GetMood(player);
            Assert.AreEqual(CharacterMood.Smile, mood, "メンタル回復時は笑顔(3)");
        }

        [Test]
        public void MentalDecrease_TriggersCrying()
        {
            var player = PlayerData.Create(0, CharacterType.Serious, false);
            player.MaxMental = 100;
            player.Mental = 80;

            // メンタル下落
            player.ApplyStatChange(0, 0, -10, 0, applyCharacterPassives: false);

            var mood = CharacterMoodManager.GetMood(player);
            Assert.AreEqual(CharacterMood.Crying, mood, "メンタル下落時は涙目(1)");
        }

        [Test]
        public void MentalDecrease_WithOtherStatIncrease_StaysCrying()
        {
            var player = PlayerData.Create(0, CharacterType.Rich, false);
            player.MaxMental = 100;
            player.Mental = 80;

            // メンタル下落
            player.ApplyStatChange(0, 0, -10, 0, applyCharacterPassives: false);
            Assert.AreEqual(CharacterMood.Crying, CharacterMoodManager.GetMood(player), "メンタル下落時は涙目(1)");

            // その後にお金が増加しても涙目を維持
            player.ApplyStatChange(20, 0, 0, 0, applyCharacterPassives: false);
            Assert.AreEqual(CharacterMood.Crying, CharacterMoodManager.GetMood(player), "メンタル下落中は他ステータスが上がっても涙目(1)を維持");
        }

        [Test]
        public void LowMental_WithOtherStatIncrease_StaysCrying()
        {
            var player = PlayerData.Create(0, CharacterType.Rich, false);
            player.MaxMental = 50;
            player.Mental = 15; // 50%以下

            Assert.AreEqual(CharacterMood.Crying, CharacterMoodManager.GetMood(player), "上昇前は涙目(1)");

            // お金増加（パラメータ上昇）しても涙目を維持
            player.ApplyStatChange(10, 0, 0, 0, applyCharacterPassives: false);
            Assert.AreEqual(CharacterMood.Crying, CharacterMoodManager.GetMood(player), "メンタル50%以下は他ステータスが上がっても涙目(1)を維持");
        }

        [Test]
        public void CharacterVisualAssets_Prefixes_MatchFileNames()
        {
            Assert.AreEqual("Tasyumi", CharacterVisualAssets.GetFilePrefix(CharacterType.Hobbyist));
            Assert.AreEqual("Majime", CharacterVisualAssets.GetFilePrefix(CharacterType.Serious));
            Assert.AreEqual("Taiiku", CharacterVisualAssets.GetFilePrefix(CharacterType.Athletic));
            Assert.AreEqual("Kanemochi", CharacterVisualAssets.GetFilePrefix(CharacterType.Rich));
            Assert.AreEqual("Tensai", CharacterVisualAssets.GetFilePrefix(CharacterType.Genius));
        }

        [Test]
        public void PlayerIdentity_Assigns1P2PNaming_ToAllPlayers()
        {
            var p1 = PlayerData.Create(0, CharacterType.Hobbyist, false);
            var p2 = PlayerData.Create(1, CharacterType.Serious, true); // NPC
            var players = new[] { p1, p2 };

            PlayerIdentity.Apply(players);

            Assert.AreEqual("1P", p1.Name);
            Assert.AreEqual("2P", p2.Name);
            Assert.AreEqual("1P（多趣味系）", PlayerIdentity.FormatHudLabel(p1));
            Assert.AreEqual("2P（真面目系）", PlayerIdentity.FormatHudLabel(p2));
        }

        [Test]
        public void PlayerPieceLabel_AttachesOverheadBadge()
        {
            var pieceGo = new GameObject("TestPiece");
            var sr = pieceGo.AddComponent<SpriteRenderer>();
            var p2 = PlayerData.Create(1, CharacterType.Serious, true);

            Board.PlayerPieceLabel.Attach(pieceGo.transform, p2);

            var badge = pieceGo.transform.Find("PieceBadge");
            Assert.IsNotNull(badge, "PieceBadge が生成されること");
            Assert.Greater(badge.localPosition.y, 0f, "バッジは駒の上部に配置されること");

            var label = badge.Find("PieceLabel")?.GetComponent<TMPro.TextMeshPro>();
            Assert.IsNotNull(label, "PieceLabel (TextMeshPro) が生成されること");
            Assert.AreEqual("2P", label.text, "NPC は 2P と頭上に表示されること");

            Object.DestroyImmediate(pieceGo);
        }
    }
}
