using Sunfire.Ansi;
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
    int StyleId
)
{
    public static readonly SVCell Blank;

    static SVCell()
    {
        var (id, width) = GlyphFactory.GetGlyphIds(" ").First();
        var styleId = StyleFactory.GetStyleId((null, null, SAnsiProperty.None));

        Blank = new(id, width, styleId);
    }
}
