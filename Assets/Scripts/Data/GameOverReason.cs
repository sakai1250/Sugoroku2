namespace Sugoroku.Data
{
    public enum GameOverReason
    {
        None,
        Bankruptcy,  // 所持金 <= 0
        Missing,     // メンタル == 0
        Expelled     // IF == 0（留年）
    }
}
