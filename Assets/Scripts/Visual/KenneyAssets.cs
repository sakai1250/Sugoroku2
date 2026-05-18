using UnityEngine;
using Sugoroku.Data;

namespace Sugoroku.Visual
{
    /// <summary>
    /// Kenney アセットパックのパス定義と Resources ロード。
    /// 実行時ロード用に Tools → Sugoroku → Kenney → Sync to Resources を実行してください。
    /// </summary>
    public static class KenneyAssets
    {
        public const string ResourcesRoot = "ThirdParty/Kenney";

        public static class BoardgamePack
        {
            public const string PiecesRoot = ResourcesRoot + "/BoardgamePack/Pieces";
            public const string AudioDieThrow = ResourcesRoot + "/BoardgamePack/Audio/dieThrow1";
            public const string DiceRoot = ResourcesRoot + "/BoardgamePack/Dice";
        }

        public static class UiPack
        {
            public const string ButtonFlat = ResourcesRoot + "/UIPack/button_rectangle_flat";
            public const string ButtonDepth = ResourcesRoot + "/UIPack/button_rectangle_depth_flat";
            public const string ButtonConfirm = ResourcesRoot + "/UIPack/button_rectangle_depth_gradient";
            public const string SliderTrack = ResourcesRoot + "/UIPack/slide_horizontal_grey";
            public const string SliderFill = ResourcesRoot + "/UIPack/slide_horizontal_color";
        }

        public static class GameIcons
        {
            public const string Basket = ResourcesRoot + "/GameIcons/basket";
            public const string Trophy = ResourcesRoot + "/GameIcons/trophy";
            public const string Star = ResourcesRoot + "/GameIcons/star";
            public const string Warning = ResourcesRoot + "/GameIcons/warning";
            public const string Gear = ResourcesRoot + "/GameIcons/gear";
            public const string Home = ResourcesRoot + "/GameIcons/home";
            public const string Pause = ResourcesRoot + "/GameIcons/pause";
        }

        public static class InterfaceSounds
        {
            public const string Click = ResourcesRoot + "/InterfaceSounds/click_001";
            public const string Confirm = ResourcesRoot + "/InterfaceSounds/confirmation_001";
            public const string Error = ResourcesRoot + "/InterfaceSounds/error_001";
        }

        public static class ToonCharacters
        {
            public const string Root = ResourcesRoot + "/ToonCharacters";
        }

        /// <summary>dieWhite1〜6 など、1〜6 の面スプライト配列を返す。</summary>
        public static Sprite[] LoadDiceFaces(string namePrefix = "dieWhite")
        {
            var faces = new Sprite[6];
            var all = Resources.LoadAll<Sprite>(BoardgamePack.DiceRoot);
            if (all == null || all.Length == 0)
                all = Resources.LoadAll<Sprite>("KenneyDice");

            for (int face = 1; face <= 6; face++)
            {
                string key = $"{namePrefix}{face}";
                foreach (var sprite in all)
                {
                    if (sprite != null && sprite.name == key)
                    {
                        faces[face - 1] = sprite;
                        break;
                    }
                }

                if (faces[face - 1] == null)
                    faces[face - 1] = LoadSprite($"{BoardgamePack.DiceRoot}/{key}");
            }

            return faces;
        }

        public static Sprite LoadSprite(string resourcesPathWithoutExtension)
        {
            var sprite = Resources.Load<Sprite>(resourcesPathWithoutExtension);
            if (sprite != null) return sprite;

            foreach (var s in Resources.LoadAll<Sprite>(resourcesPathWithoutExtension))
                return s;

            return null;
        }

        public static AudioClip LoadAudio(string resourcesPathWithoutExtension) =>
            Resources.Load<AudioClip>(resourcesPathWithoutExtension);

        public static Sprite GetDiceHudIcon(int face = 6)
        {
            var faces = LoadDiceFaces("dieWhite");
            int index = Mathf.Clamp(face, 1, 6) - 1;
            if (faces.Length > index && faces[index] != null)
                return faces[index];
            return LoadSprite($"{BoardgamePack.DiceRoot}/dieWhite6");
        }

        public static Sprite GetResourceIcon(ResourceIcon kind) => kind switch
        {
            ResourceIcon.Money  => LoadSprite(GameIcons.Basket),
            ResourceIcon.IfScore => LoadSprite(GameIcons.Trophy),
            ResourceIcon.Mental => LoadSprite(GameIcons.Warning),
            ResourceIcon.Virtue => LoadSprite(GameIcons.Star),
            ResourceIcon.Dice   => GetDiceHudIcon(6),
            ResourceIcon.Menu   => LoadSprite(GameIcons.Gear),
            _ => null
        };

        public static Sprite GetCharacterPortrait(CharacterType type) =>
            OriginalcharAssets.GetSprite(type) ?? LoadSprite(GameIcons.Star);

        public enum ResourceIcon
        {
            Money, IfScore, Mental, Virtue, Dice, Menu
        }
    }
}
