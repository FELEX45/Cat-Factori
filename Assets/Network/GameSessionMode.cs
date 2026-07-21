/// <summary>
/// Режим текущей игровой сессии: офлайн (кнопка разработчика) или онлайн-лобби.
/// </summary>
public static class GameSessionMode
{
    public enum Mode
    {
        Offline,
        Online
    }

    public static Mode Current { get; private set; } = Mode.Offline;

    public static bool IsOnline => Current == Mode.Online;

    public static void SetOffline()
    {
        Current = Mode.Offline;
    }

    public static void SetOnline()
    {
        Current = Mode.Online;
    }
}
