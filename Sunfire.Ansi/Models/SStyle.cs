namespace Sunfire.Ansi.Models;

public readonly record struct SStyle(
    SColor? ForegroundColor = null,
    SColor? BackgroundColor = null,
    SAnsiProperty Properties = SAnsiProperty.None,
    (int X, int Y)? CursorPosition = null
);
