using Sunfire.Ansi.Models;
using Sunfire.Tui.Enums;

namespace Sunfire.Tui.Models;

public record struct SVCell(
    string Data,
    SColor? ForegroundColor,
    SColor? BackgroundColor,
    SAnsiProperty Properties
) {
    public static readonly SVCell Blank = new(" ", new SColor() { R = 255, B = 255, G = 255 }, null, SAnsiProperty.None);
}
