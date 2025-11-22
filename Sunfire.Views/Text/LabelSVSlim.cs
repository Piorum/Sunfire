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

    public Task<bool> Arrange()
    {
        if (Dirty)
        {
            Dirty = false;

            return Task.FromResult(true);
        }
        return Task.FromResult(false);
    }

    public Task Draw(SVContext context)
    {
        int textLen = Text.Length;
        int startX = 0;
        if(Alignment == Direction.Right)
        {
            startX = SizeX - textLen;
        }

        for (int y = 0; y < SizeY; y++)
        {
            for (int x = 0; x < SizeX; x++)
            {
                int textIndex = x - startX;

                char charToDraw = textIndex >= 0 && textIndex < textLen 
                    ? Text[textIndex] 
                    :  ' ';

                context[x, y] = context[x, y] with
                {
                    Data = charToDraw,
                    ForegroundColor = TextColor,
                    Properties = TextProperties
                };
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
