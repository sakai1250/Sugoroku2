using System.Collections.Generic;

namespace Sugoroku.Data
{
    /// <summary>キャラ×ランクで分岐する進路エンディングのカタログ(5キャラ×4ランク=20通り)。</summary>
    public static class CareerOutcomeCatalog
    {
        public static readonly string[] Ranks = { "S", "A", "B", "C" };

        public readonly struct Entry
        {
            public readonly CharacterType Character;
            public readonly string Rank;
            public readonly string CareerPath;
            public readonly string Subtitle;

            public Entry(CharacterType character, string rank, string careerPath, string subtitle)
            {
                Character = character;
                Rank = rank;
                CareerPath = careerPath;
                Subtitle = subtitle;
            }
        }

        private static readonly Dictionary<(CharacterType, string), Entry> _table = BuildTable();

        private static Dictionary<(CharacterType, string), Entry> BuildTable()
        {
            var entries = new[]
            {
                new Entry(CharacterType.Hobbyist, "S", "自由人教授", "趣味で始めた研究がいつの間にか看板ゼミに——好きなことだけで生きていく"),
                new Entry(CharacterType.Serious,  "S", "叩き上げ教授", "誰よりも講義に出て誰よりも書いた、正攻法の頂点"),
                new Entry(CharacterType.Athletic, "S", "体育会系教授", "根性で乗り切った先に椅子があった——研究室の朝礼は今日も挨拶から"),
                new Entry(CharacterType.Rich,     "S", "私財投入型教授", "研究費は全部自腹、その分誰にも口出しさせない研究室運営"),
                new Entry(CharacterType.Genius,   "S", "異端の若手教授", "ジャーナル一本の破壊力で椅子を勝ち取った、次は何をやらかすか誰も知らない"),

                new Entry(CharacterType.Hobbyist, "A", "自由出勤の研究職", "フレックスと裁量労働をフル活用、定時にはもういない"),
                new Entry(CharacterType.Serious,  "A", "堅実な主任研究員", "地道な積み重ねが評価された、王道のキャリアパス"),
                new Entry(CharacterType.Athletic, "A", "現場叩き上げの技術職", "泥臭い実験を厭わなかった姿勢が買われた"),
                new Entry(CharacterType.Rich,     "A", "コネ抜きの実力入社", "金で解決してきたはずが、気づけば実力もついていた"),
                new Entry(CharacterType.Genius,   "A", "花形プロジェクトのエース", "たった一つの成果で社内評価が跳ね上がった、次のヒットはまだ見えない"),

                new Entry(CharacterType.Hobbyist, "B", "ゆるく働く一般職", "研究は趣味に格下げ、仕事はほどほどに"),
                new Entry(CharacterType.Serious,  "B", "堅実な一般企業社員", "アカデミアは諦めたが、社会人としては優秀"),
                new Entry(CharacterType.Athletic, "B", "体力勝負の営業職", "研究室のノリのままフィールドに出た"),
                new Entry(CharacterType.Rich,     "B", "実家の伝手で入った企業", "自分の実力かどうかは、あえて聞かない"),
                new Entry(CharacterType.Genius,   "B", "宝の持ち腐れの一般職", "光る瞬間はあったのに、続かなかった"),

                new Entry(CharacterType.Hobbyist, "C", "趣味人のまま実家へ", "研究は良い思い出、店番も悪くない"),
                new Entry(CharacterType.Serious,  "C", "燃え尽きて実家へ", "真面目にやりすぎた反動が今頃来ている"),
                new Entry(CharacterType.Athletic, "C", "実家の店を継ぐ跡取り", "体力だけは誰にも負けない、店は繁盛するだろう"),
                new Entry(CharacterType.Rich,     "C", "結局実家が一番安定", "散財の果てに気づいた、実家が最強のセーフティネット"),
                new Entry(CharacterType.Genius,   "C", "早すぎた才能、実家で燻る", "誰も理解してくれなかった、店の常連客以外は"),
            };

            var table = new Dictionary<(CharacterType, string), Entry>();
            foreach (var e in entries)
                table[(e.Character, e.Rank)] = e;
            return table;
        }

        /// <summary>該当エントリを返す。未登録の組み合わせはランクC相当のフォールバックを返す。</summary>
        public static Entry Resolve(CharacterType character, string rank)
        {
            if (_table.TryGetValue((character, rank), out var entry)) return entry;
            return new Entry(character, rank, "実家稼業", "ポスドク修羅は遠い——実家の店を継ぐ");
        }

        /// <summary>図鑑UI用: ランクS→A→B→C、各ランク内はCharacterTypeのenum順で全20件を列挙する。</summary>
        public static IEnumerable<Entry> EnumerateAll()
        {
            foreach (var rank in Ranks)
                foreach (CharacterType character in System.Enum.GetValues(typeof(CharacterType)))
                    yield return Resolve(character, rank);
        }
    }
}
