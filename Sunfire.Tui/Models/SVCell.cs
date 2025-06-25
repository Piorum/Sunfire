using Sunfire.Ansi.Models;

namespace Sunfire.Tui.Models;

/// <summary>
/// Contains all information needed to render cell (x, y) (determined by placement in buffer)
/// </summary>
/// <param name="Data">UTF8 Char to display.</param>
/// <param name="ForegroundColor">Foreground color cell should be.</param>
/// <param name="BackgroundColor">Background color cell should be.</param>
/// <param name="Properties">Ansi properties cell should have.</param>
/// <param name="RichData">Ref to rich data that should be displayed at cell's position</param>
public record struct SVCell(
    char Data,
    SColor? ForegroundColor,
    SColor? BackgroundColor,
    SAnsiProperty Properties,
    RichDataRef? RichData
)
{
    public static readonly SVCell Blank = new(' ', null, null, SAnsiProperty.None, null);
}
