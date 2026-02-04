using Sunfire.Ansi.Models;
using Sunfire.Glyph;

namespace Sunfire.Tui.Models;

/// <summary>
/// Contains all information needed to render cell (x, y) (determined by placement in buffer)
/// </summary>
/// <param name="Data">UTF8 Char to display.</param>
/// <param name="ForegroundColor">Foreground color cell should be.</param>
/// <param name="BackgroundColor">Background color cell should be.</param>
/// <param name="Properties">Ansi properties cell should have.</param>
public record struct SVCell(
    int GlyphId,
    byte Width,
    SColor? ForegroundColor,
    SColor? BackgroundColor,
    SAnsiProperty Properties
)
{
    public static readonly SVCell Blank;

    static SVCell()
    {
        var blankGlyph = GlyphFactory.GetGlyphs(" ").First();

        Blank = new(blankGlyph.id, blankGlyph.width, null, null, SAnsiProperty.None);
    }
}
