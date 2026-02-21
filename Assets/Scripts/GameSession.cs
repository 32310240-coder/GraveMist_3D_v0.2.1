public static class GameSession
{
    // ---- 設定（選択シーン → MainScene）----
    public static int PlayerCount = 4; // 2〜4

    // ---- 結果（MainScene → WinScene）----
    public static int WinnerIndex = -1; // 0=1P, 1=2P...

    public static void ResetResult()
    {
        WinnerIndex = -1;
    }

    public static void ResetAll()
    {
        PlayerCount = 4;
        WinnerIndex = -1;
    }
}