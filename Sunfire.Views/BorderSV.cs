using System.Text;
using Sunfire.Tui.Enums;
using Sunfire.Tui.Models;
using Sunfire.Tui.Interfaces;
using Sunfire.Ansi.Models;
using Sunfire.Views.Text;
using Sunfire.Glyph;
using Sunfire.Glyph.Models;
using Sunfire.Ansi;

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

    //Blank
    private static readonly (int id, byte width) Blank = GlyphFactory.GetGlyphIds(" ").First();

    //Corners
    //Rounded
    private static readonly (int id, byte width) TopLeft = GlyphFactory.GetGlyphIds("╭").First();
    private static readonly (int id, byte width) TopRight = GlyphFactory.GetGlyphIds("╮").First();
    private static readonly (int id, byte width) BottomLeft = GlyphFactory.GetGlyphIds("╰").First();
    private static readonly (int id, byte width) BottomRight = GlyphFactory.GetGlyphIds("╯").First();
    //Squared
    //private const char TopLeft = (char)9484;
    //private const char TopRight = (char)9488;
    //private const char BottomLeft = (char)9492;
    //private const char BottomRight = (char)9496;

    //Sides
    private static readonly (int id, byte width) Horizontal = GlyphFactory.GetGlyphIds("─").First();
    private static readonly (int id, byte width) Vertical = GlyphFactory.GetGlyphIds("│").First();
    private static readonly (int id, byte width) TitleLeft = GlyphFactory.GetGlyphIds("╴").First();
    private static readonly (int id, byte width) TitleRight = GlyphFactory.GetGlyphIds("╶").First();
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

    protected virtual Task OnArrange()
    {
        var templateStyleId = StyleFactory.GetStyleId((BorderColor, BackgroundColor, SAnsiProperty.None));
        templateCell = new(
            Blank.id,
            Blank.width,
            templateStyleId
        );
        
        borderBuffer = new(SizeX, SizeY);

        if (borderBuffer.Height > 2)
            for (int x = 1; x < SizeX - 1; x++)
            {
                SVCell horizontalCell = new(Horizontal.id, Horizontal.width, templateStyleId);
                borderBuffer[x, 0] = horizontalCell;
                borderBuffer[x, SizeY - 1] = horizontalCell;
            }
        if (borderBuffer.Width > 2)
            for (int y = 1; y < SizeY - 1; y++)
            {
                SVCell verticalCell = new(Vertical.id, Vertical.width, templateStyleId);
                borderBuffer[0, y] = verticalCell;
                borderBuffer[SizeX - 1, y] = verticalCell;
            }
        if (SizeX > 0 && SizeY > 0)
        {
            borderBuffer[0, 0] = new(TopLeft.id, TopLeft.width, templateStyleId);
            borderBuffer[SizeX - 1, 0] = new(TopRight.id, TopRight.width, templateStyleId);
            borderBuffer[0, SizeY - 1] = new(BottomLeft.id, BottomLeft.width, templateStyleId);
            borderBuffer[SizeX - 1, SizeY - 1] = new(BottomRight.id, BottomRight.width, templateStyleId);
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
            borderBuffer[1, 0] = new(TitleLeft.id, TitleLeft.width, templateStyleId);
            borderBuffer[2 + Math.Min(TitleLabel.Segments?.Sum(e => e.Text.Length) ?? 0, SizeX - 4), 0] = new(TitleRight.id, TitleRight.width, templateStyleId);
        }

        //Subpane setup
        SubView.SizeX = SizeX - 2;
        SubView.SizeY = SizeY - 2;
        SubView.OriginX = OriginX + 1;
        SubView.OriginY = OriginY + 1;

        if (TitleLabel is not null)
        {
            TitleLabel.SizeX = Math.Min(TitleLabel.Segments?.Sum(e => e.Text.Length) ?? 0, SizeX - 4);
            TitleLabel.SizeY = Math.Min(1, SizeY);
            TitleLabel.OriginX = OriginX + 2;
            TitleLabel.OriginY = OriginY;
            TitleLabel.Alignment = Direction.Right;
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
