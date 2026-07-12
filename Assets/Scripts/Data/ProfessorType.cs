using UnityEngine;

namespace Sugoroku.Data
{
    public enum ProfessorType
    {
        None,
        Laissez,
        DrillSergeant,
        Virtuous
    }

    public static class ProfessorTypeExtensions
    {
        public static string DisplayName(this ProfessorType type) => type switch
        {
            ProfessorType.Laissez       => "放任型教授",
            ProfessorType.DrillSergeant => "鬼軍曹教授",
            ProfessorType.Virtuous      => "人格者教授",
            _                           => "指導教員なし"
        };
    }

    /// <summary>指導教員による常時補正。CharacterTagAffinityとは独立した層として、EventManagerのイベント選択肢変動にのみ適用する。</summary>
    public static class ProfessorEffectRules
    {
        public static (int money, int ifScore, int mental, int virtue) Apply(
            ProfessorType professor, int money, int ifScore, int mental, int virtue)
        {
            switch (professor)
            {
                case ProfessorType.Laissez:
                    if (ifScore > 0) ifScore -= Mathf.Max(0, Mathf.RoundToInt(ifScore * 0.10f));
                    if (mental < 0) mental = Mathf.RoundToInt(mental * 0.85f);
                    break;
                case ProfessorType.DrillSergeant:
                    if (ifScore > 0) ifScore += Mathf.RoundToInt(ifScore * 0.15f);
                    if (mental < 0) mental = Mathf.RoundToInt(mental * 1.15f);
                    break;
                case ProfessorType.Virtuous:
                    if (virtue > 0) virtue += Mathf.RoundToInt(virtue * 0.20f);
                    break;
            }
            return (money, ifScore, mental, virtue);
        }
    }
}
