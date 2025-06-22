using Sunfire.Ansi.Models;
using SunfireFramework.Enums;
using SunfireFramework.Rendering;

namespace SunfireFramework.Views.TextBoxes;

public class LabelSVSlim : ISunfireView
{
    public int OriginX { set; get; }
    public int OriginY { set; get; }
    public int SizeX { set; get; }
    public int SizeY { set; get; }

    public bool Dirty { set; get; }

    public SAnsiProperty TextProperties = SAnsiProperty.None;
    public SVLabelProperty LabelProperties = SVLabelProperty.None;
    public SVDirection Alignment = SVDirection.Left;

    public SColor? TextColor = new() { R = 255, G = 255, B = 255 };
    public SColor? BackgroundColor = null;

    public string Text = "";

    private string compiledText = "";
    private SVCell templateCell = SVCell.Blank;

    public async Task<bool> Arrange()
    {
        if (Dirty)
        {
            await OnArrange();
            Dirty = false;

            return true; //Work Done
        }

        return false; //No Work Done
    }

    private Task OnArrange()
    {
        var maxSize = SizeX * SizeY;

        compiledText = Alignment switch
        {
            SVDirection.Left => Text[..Math.Min(Text.Length, maxSize)] + new string(' ', Math.Max(0, maxSize - Text.Length)),
            SVDirection.Right => new string(' ', Math.Max(0, maxSize - Text.Length)) + Text[^Math.Min(Text.Length, maxSize)..],
            _ => throw new InvalidOperationException("Label has invalid alignment direction.")
        };

        templateCell = new SVCell
        {
            ForegroundColor = TextColor,
            BackgroundColor = BackgroundColor,
            Properties = TextProperties
        };

        return Task.CompletedTask;
    }

    public Task Draw(SVContext context)
    {
        string[] rows = SizeX > 0
            ? [.. compiledText.Chunk(SizeX).Select(x => new string(x))]
            : [];
        for (int y = 0; y < rows.Length; y++)
        {
            var row = rows[y];
            for (int x = 0; x < SizeX; x++)
            {
                context[x, y] = templateCell with { Data = row[x].ToString() };
            }
        }
        return Task.CompletedTask;
    }

    public Task Invalidate()
    {
        Dirty = true;
        return Task.CompletedTask;
    }
}
