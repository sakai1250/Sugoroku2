namespace Sugoroku.Data
{
    public enum GameOverReason
    {
        None,
        Bankruptcy,  // 所持金 <= 0
        Missing,     // メンタル == 0
        Expelled     // 旧仕様: IF 0 による留年
    }
}
