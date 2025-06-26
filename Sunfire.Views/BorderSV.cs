using Sunfire.Tui.Enums;
using Sunfire.Tui.Models;
using Sunfire.Tui.Interfaces;
using Sunfire.Ansi.Models;
using Sunfire.Views.Text;

namespace Sunfire.Views;

public class BorderSV : IRelativeSunfireView
{
    public int X => SubView.X;
    public int Y => SubView.Y;
    public int Z => SubView.Z;

    public FillStyle FillStyleX => SubView.FillStyleX;
    public FillStyle FillStyleY => SubView.FillStyleY;
    public int StaticX => SubView.StaticX + 2;
    public int StaticY => SubView.StaticY + 2;
    public float PercentX => SubView.PercentX;
    public float PercentY => SubView.PercentY;

    public int MinX => SubView.MinX + 2;
    public int MinY => SubView.MinY + 2;

    public int OriginX { set; get; }
    public int OriginY { set; get; }
    public int SizeX { set; get; }
    public int SizeY { set; get; }

    public bool Dirty { set; get; }

    public SColor? BorderColor { set; get; } = null;
    public SColor? BackgroundColor { set; get; } = null;

    required public IRelativeSunfireView SubView { set; get; }
    public LabelSVSlim? TitleLabel { set; get; }

    private SVBuffer? borderBuffer;
    private SVCell templateCell = SVCell.Blank;

    //Corners
    //Rounded
    private const char TopLeft = '╭';
    private const char TopRight = '╮';
    private const char BottomLeft = '╰';
    private const char BottomRight = '╯';
    //Squared
    //private const char TopLeft = (char)9484;
    //private const char TopRight = (char)9488;
    //private const char BottomLeft = (char)9492;
    //private const char BottomRight = (char)9496;

    //Sides
    private const char Horizontal = '─';
    private const char Vertical = '│';
    private const char TitleLeft = '╴';
    private const char TitleRight = '╶';

    public async Task<bool> Arrange()
    {
        bool borderUpdated = false;

        if (Dirty)
        {
            await OnArrange();
            Dirty = false;
            borderUpdated = true;
        }

        bool workDone = await SubView.Arrange();
        if (TitleLabel is not null)
            await TitleLabel.Arrange();

        return workDone || borderUpdated;
    }

    private Task OnArrange()
    {
        templateCell = new SVCell
        {
            Data = ' ',
            ForegroundColor = BorderColor,
            BackgroundColor = BackgroundColor
        };
        
        borderBuffer = new(SizeX, SizeY);

        if (borderBuffer.Height > 2)
            for (int x = 1; x < SizeX - 1; x++)
            {
                borderBuffer[x, 0] = templateCell with { Data = Horizontal };
                borderBuffer[x, SizeY - 1] = templateCell with { Data = Horizontal };
            }
        if (borderBuffer.Width > 2)
            for (int y = 1; y < SizeY - 1; y++)
            {
                borderBuffer[0, y] = templateCell with { Data = Vertical };
                borderBuffer[SizeX - 1, y] = templateCell with { Data = Vertical };
            }
        if (SizeX > 0 && SizeY > 0)
        {
            borderBuffer[0, 0] = templateCell with { Data = TopLeft };
            borderBuffer[SizeX - 1, 0] = templateCell with { Data = TopRight };
            borderBuffer[0, SizeY - 1] = templateCell with { Data = BottomLeft };
            borderBuffer[SizeX - 1, SizeY - 1] = templateCell with { Data = BottomRight };
        }

        for (int y = 1; y < SizeY - 1; y++)
        {
            for (int x = 1; x < SizeX - 1; x++)
            {
                borderBuffer[x, y] = templateCell;
            }
        }

        if (TitleLabel is not null && SizeX >= 4 && SizeY > 0)
        {
            borderBuffer[1, 0] = templateCell with { Data = TitleLeft };
            borderBuffer[2 + Math.Min(TitleLabel.Text.Length, SizeX - 4), 0] = templateCell with { Data = TitleRight };
        }

        //Subpane setup
        SubView.SizeX = SizeX - 2;
        SubView.SizeY = SizeY - 2;
        SubView.OriginX = OriginX + 1;
        SubView.OriginY = OriginY + 1;

        if (TitleLabel is not null)
        {
            TitleLabel.SizeX = Math.Min(TitleLabel.Text.Length, SizeX - 4);
            TitleLabel.SizeY = Math.Min(1, SizeY);
            TitleLabel.OriginX = OriginX + 2;
            TitleLabel.OriginY = OriginY;
            TitleLabel.TextColor = BorderColor;
            TitleLabel.Alignment = Direction.Right;
            TitleLabel.LabelProperties = LabelSVProperty.Trimmed;
        }

        return Task.CompletedTask;
    }

    public async Task Draw(SVContext context)
    {
        var bufferSpan = borderBuffer!.AsSpan();

        for (int y = 0; y < SizeY; y++)
        {
            for (int x = 0; x < SizeX; x++)
            {
                context[x, y] = bufferSpan[(borderBuffer.Width * y) + x];
            }
        }

        if (TitleLabel is not null)
            await TitleLabel.Draw(new(TitleLabel.OriginX, TitleLabel.OriginY, context.Buffer));

        await SubView.Draw(new(SubView.OriginX, SubView.OriginY, context.Buffer));
    }

    public async Task Invalidate()
    {
        Dirty = true;
        await SubView.Invalidate();
        if(TitleLabel is not null)
            await TitleLabel.Invalidate();
    }

}
