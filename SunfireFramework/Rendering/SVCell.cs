using Sunfire.Ansi.Models;
using SunfireFramework.Enums;

namespace SunfireFramework.Rendering;

public record struct SVCell(
    string Data,
    SColor? ForegroundColor,
    SColor? BackgroundColor,
    SAnsiProperty Properties
) {
    public static readonly SVCell Blank = new(" ", new SColor() { R = 255, B = 255, G = 255 }, null, SAnsiProperty.None);
}
