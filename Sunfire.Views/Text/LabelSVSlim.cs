using Sunfire.Tui.Enums;
using Sunfire.Tui.Models;
using Sunfire.Tui.Interfaces;
using Sunfire.Ansi.Models;

namespace Sunfire.Views.Text;

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

    public SColor? TextColor = null;

    public string Text = "";

    private string compiledText = "";

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
        if (blankSpaceSize > 0)
        {
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
        }
        else
        {
            compiledText = Text;
        }

        return Task.CompletedTask;
    }

    public Task Draw(SVContext context)
    {
        int index;
        switch (Alignment)
        {
            case Direction.Left:
                index = 0;
                for (int y = 0; y < SizeY; y++)
                {
                    for (int x = 0; x < SizeX; x++)
                    {
                        context[x, y] = context[x, y] with { Data = compiledText[index], ForegroundColor = TextColor, Properties = TextProperties };
                        index++;
                    }
                }
                break;
            case Direction.Right:
                index = compiledText.Length - 1;
                for (int y = SizeY - 1; y >= 0; y--)
                {
                    for (int x = SizeX - 1; x >= 0; x--)
                    {
                        context[x, y] = context[x, y] with { Data = compiledText[index], ForegroundColor = TextColor, Properties = TextProperties };
                        index--;
                    }
                }
                break;
        }
        return Task.CompletedTask;
    }

    public Task Invalidate()
    {
        Dirty = true;
        return Task.CompletedTask;
    }
}
