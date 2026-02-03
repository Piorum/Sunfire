using Sunfire.Ansi.Models;
using Sunfire.Glyph;
using Sunfire.Glyph.Models;

namespace Sunfire.Tui.Models;

/// <summary>
/// Contains all information needed to render cell (x, y) (determined by placement in buffer)
/// </summary>
/// <param name="Data">UTF8 Char to display.</param>
/// <param name="ForegroundColor">Foreground color cell should be.</param>
/// <param name="BackgroundColor">Background color cell should be.</param>
/// <param name="Properties">Ansi properties cell should have.</param>
public record struct SVCell(
    GlyphInfo Data,
    SColor? ForegroundColor,
    SColor? BackgroundColor,
    SAnsiProperty Properties
)
{
    public static readonly SVCell Blank = new(GlyphFactory.GetGlyphs(" ").First(), null, null, SAnsiProperty.None);
}
