using SunfireFramework.Enums;

namespace SunfireFramework.Rendering;

public record struct SVCell(
    char Char,
    SVColor ForegroundColor,
    SVColor BackgroundColor,
    SVTextProperty Properties
) {
    public static readonly SVCell Blank = new(' ', new SVColor() { R = 255, B = 255, G = 255 }, new SVColor() { R = 0, B = 0, G = 0 }, SVTextProperty.None);
}
