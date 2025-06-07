using SunfireInputParser.Enums;

namespace SunfireInputParser.Input;

public readonly record struct TerminalInput
(
    InputType Type,
    DateTime CreationTime,

    Modifier Modifiers = Modifier.None,

    //Keyboard
    ConsoleKey? Key = null,
    char? Char = null,

    //Mouse
    MouseAction? MouseKey = null,
    int? X = null,
    int? Y = null,
    int? ScrollDelta = null
);
