namespace Sunfire;

public static class Keybindings
{
    public static ConsoleKeyInfo ExitKey { private set; get; } = new('q', ConsoleKey.Q, false, false, true);

    public static bool Equals(ConsoleKeyInfo key1, ConsoleKeyInfo key2) =>
        key1.Key == key2.Key && key1.Modifiers == key2.Modifiers;
}
