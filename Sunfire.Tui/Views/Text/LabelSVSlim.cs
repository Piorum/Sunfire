using Sunfire.Ansi.Models;
using Sunfire.Tui.Enums;
using Sunfire.Tui.Models;

namespace Sunfire.Tui.Views.Text;

public class LabelSVSlim : ISunfireView
{
    public int OriginX { set; get; }
    public int OriginY { set; get; }
    public int SizeX { set; get; }
    public int SizeY { set; get; }

    public bool Dirty { set; get; }

    public SAnsiProperty TextProperties = SAnsiProperty.None;
    public LabelSVProperty LabelProperties = LabelSVProperty.None;
    public Direction Alignment = Direction.Left;

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
        var blankSpaceSize = (SizeX * SizeY) - Text.Length;
        var blankString = new string(' ', blankSpaceSize);

        switch (Alignment)
        {
            case Direction.Left:
                compiledText = string.Concat(Text, blankString);
                break;
            case Direction.Right:
                compiledText = string.Concat(blankString, Text);
                break;
        }

        templateCell = new SVCell
        {
            Data = " ",
            ForegroundColor = TextColor,
            BackgroundColor = BackgroundColor,
            Properties = TextProperties
        };

        return Task.CompletedTask;
    }

    public Task Draw(SVContext context)
    {
        var index = 0;
        for (int y = 0; y < SizeY; y++)
        {
            for (int x = 0; x < SizeX; x++)
            {
                context[x, y] = templateCell with { Data = compiledText[index].ToString() };
                index++;
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
