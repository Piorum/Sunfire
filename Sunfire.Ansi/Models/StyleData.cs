namespace Sunfire.Ansi.Models;

public record StyleData(
    SColor? ForegroundColor = null,
    SColor? BackgroundColor = null,
    SAnsiProperty Properties = SAnsiProperty.None);
