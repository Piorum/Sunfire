using Sunfire.Input.Enums;

namespace Sunfire.Input.Types;

public readonly record struct TerminalInput
{
    public Key Key { get; }
    public InputData InputData { get; }

    private TerminalInput(Key key, InputData inputData)
    {
        Key = key;
        InputData = inputData;
    }

    public static TerminalInput KeyboardInput(ConsoleKey keyboardKey, char utfChar, Modifier? modifiers = null) =>
        new(Key.KeyboardBind(keyboardKey, modifiers), InputData.KeyboardData(utfChar));
    public static TerminalInput MouseInput(MouseAction mouseKey, int x, int y, int? scrollDelta = null, Modifier? modifiers = null) =>
        new(Key.MouseBind(mouseKey, modifiers), InputData.MouseData(x, y, scrollDelta));
};