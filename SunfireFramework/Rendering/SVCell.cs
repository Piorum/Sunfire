using SunfireFramework.Enums;

namespace SunfireFramework.Rendering;

public record struct SVCell(
    string Data,
    SVColor? ForegroundColor,
    SVColor? BackgroundColor,
    SVTextProperty Properties
) {
    public static readonly SVCell Blank = new(" ", new SVColor() { R = 255, B = 255, G = 255 }, null, SVTextProperty.None);
}
