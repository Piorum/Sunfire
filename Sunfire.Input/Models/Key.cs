using Sunfire.Input.Enums;

namespace Sunfire.Input.Models;

public readonly record struct Key
{
    public InputType InputType { get; }
    public Modifier Modifiers { get; } = Modifier.None;

    public ConsoleKey? KeyboardKey { get; } = null;

    public MouseAction? MouseKey { get; } = null;

    private Key(InputType inputType, Modifier modifiers, ConsoleKey? key = null, MouseAction? mouseKey = null)
    {
        InputType = inputType;
        Modifiers = modifiers;

        KeyboardKey = key;

        MouseKey = mouseKey;
    }

    public static Key KeyboardBind(ConsoleKey key, Modifier? modifiers = null) =>
        new(InputType.Keyboard, modifiers ?? Modifier.None, key: key);
    public static Key MouseBind(MouseAction mouseKey, Modifier? modifiers = null) =>
        new(InputType.Mouse, modifiers ?? Modifier.None, mouseKey: mouseKey);

}
