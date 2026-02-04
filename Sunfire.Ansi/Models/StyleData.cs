namespace Sunfire.Ansi.Models;

public readonly record struct StyleData(
    SColor? ForegroundColor = null,
    SColor? BackgroundColor = null,
    SAnsiProperty Properties = SAnsiProperty.None
);
