namespace Sunfire;

public static class Keybindings
{
    public static ConsoleKeyInfo ExitKey { private set; get; } = new('q', ConsoleKey.Q, false, false, true);

    public static ConsoleKeyInfo NavUp { private set; get; } = new('w', ConsoleKey.W, false, false, false);
    public static ConsoleKeyInfo NavOut { private set; get; } = new('a', ConsoleKey.A, false, false, false);
    public static ConsoleKeyInfo NavDown { private set; get; } = new('s', ConsoleKey.S, false, false, false);
    public static ConsoleKeyInfo NavIn { private set; get; } = new('d', ConsoleKey.D, false, false, false);

    public static ConsoleKeyInfo NavTop { private set; get; } = new('g', ConsoleKey.G, false, false, false);
    public static ConsoleKeyInfo NavBottom { private set; get; } = new('G', ConsoleKey.G, true, false, false);

    public static ConsoleKeyInfo Select { private set; get; } = new(' ', ConsoleKey.Spacebar, false, false, false);

    public static ConsoleKeyInfo ForceDelete { private set; get; } = new((char)127, ConsoleKey.Delete, true, false, false);

    public static ConsoleKeyInfo Reload { private set; get; } = new('r', ConsoleKey.R, false, true, true);

    public static bool Equals(ConsoleKeyInfo key1, ConsoleKeyInfo key2) =>
        key1.Key == key2.Key && key1.Modifiers == key2.Modifiers;
}
