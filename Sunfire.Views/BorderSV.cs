using Sunfire.Tui.Enums;
using Sunfire.Tui.Models;
using Sunfire.Tui.Interfaces;
using Sunfire.Ansi.Models;

namespace Sunfire.Views;

public class BorderSV : IRelativeSunfireView
{
    public int X => SubPane.X;
    public int Y => SubPane.Y;
    public int Z => SubPane.Z;

    public FillStyle FillStyleX => SubPane.FillStyleX;
    public FillStyle FillStyleY => SubPane.FillStyleY;
    public int StaticX => SubPane.StaticX;
    public int StaticY => SubPane.StaticY;
    public float PercentX => SubPane.PercentX;
    public float PercentY => SubPane.PercentY;

    public int OriginX { set; get; }
    public int OriginY { set; get; }
    public int SizeX { set; get; }
    public int SizeY { set; get; }

    public bool Dirty { set; get; }

    public Direction BorderSides = Direction.None;
    public Direction BorderConnections = Direction.None;

    public SColor? BorderColor { set; get; } = null;
    public SColor? BackgroundColor { set; get; } = null;

    required public PaneSV SubPane { set; get; }

    //Border buffer
    private string[] borderBuffer = [];
    private SVCell templateCell = SVCell.Blank;

    //Box building characters
    //Squared
    //private const char TopLeft = (char)9484;
    //private const char TopRight = (char)9488;
    //private const char BottomLeft = (char)9492;
    //private const char BottomRight = (char)9496;
    //Rounded
    private const char TopLeft = '╭';
    private const char TopRight = '╮';
    private const char BottomLeft = '╰';
    private const char BottomRight = '╯';

    private const char Horizontal = '─';
    private const char Vertical = '│';

    private const char HorizontalAndDown = '┬';
    private const char HorizontalAndUp = '┴';
    private const char HorizontalAndVertical = '┼';
    private const char VerticalAndLeft = '┤';
    private const char VerticalAndRight = '├';

    //Box building lookup tables
    //Connections, BorderSide, ProperCornerChar
    private static readonly Dictionary<(Direction, Direction), char> topLeftMap =
    new()
    {
        { (Direction.Top | Direction.Left, Direction.Top | Direction.Left), HorizontalAndVertical },
        { (Direction.Top | Direction.Left, Direction.Top),  HorizontalAndUp },
        { (Direction.Top | Direction.Left, Direction.Left), VerticalAndLeft },

        { (Direction.Top, Direction.Top | Direction.Left), VerticalAndRight },
        { (Direction.Top, Direction.Top),  Horizontal },
        { (Direction.Top, Direction.Left), Vertical },

        { (Direction.Left, Direction.Top | Direction.Left), HorizontalAndDown },
        { (Direction.Left, Direction.Top),  Horizontal },
        { (Direction.Left, Direction.Left), Vertical },

        { (Direction.None, Direction.Top | Direction.Left), TopLeft },
        { (Direction.None, Direction.Top),  Horizontal },
        { (Direction.None, Direction.Left), Vertical },
    };
    private static readonly Dictionary<(Direction, Direction), char> topRightMap =
    new()
    {
        { (Direction.Top | Direction.Right, Direction.Top | Direction.Right), HorizontalAndVertical },
        { (Direction.Top | Direction.Right, Direction.Top),  HorizontalAndUp },
        { (Direction.Top | Direction.Right, Direction.Right), VerticalAndRight },

        { (Direction.Top,  Direction.Top | Direction.Right), VerticalAndLeft },
        { (Direction.Top,  Direction.Top),  Horizontal },
        { (Direction.Top,  Direction.Right), Vertical },

        { (Direction.Right, Direction.Top | Direction.Right), HorizontalAndDown },
        { (Direction.Right, Direction.Top),  Horizontal },
        { (Direction.Right, Direction.Right), Vertical },

        { (Direction.None, Direction.Top | Direction.Right), TopRight },
        { (Direction.None, Direction.Top),  Horizontal },
        { (Direction.None, Direction.Right), Vertical },
    };
    private static readonly Dictionary<(Direction, Direction), char> bottomLeftMap =
    new()
    {
        { (Direction.Bottom | Direction.Left, Direction.Bottom | Direction.Left), HorizontalAndVertical },
        { (Direction.Bottom | Direction.Left, Direction.Bottom),  HorizontalAndDown },
        { (Direction.Bottom | Direction.Left, Direction.Left), VerticalAndLeft },

        { (Direction.Bottom,  Direction.Bottom | Direction.Left), VerticalAndRight },
        { (Direction.Bottom,  Direction.Bottom),  Horizontal },
        { (Direction.Bottom,  Direction.Left), Vertical },

        { (Direction.Left, Direction.Bottom | Direction.Left), HorizontalAndUp },
        { (Direction.Left, Direction.Bottom),  Horizontal },
        { (Direction.Left, Direction.Left), Vertical },

        { (Direction.None, Direction.Bottom | Direction.Left), BottomLeft },
        { (Direction.None, Direction.Bottom),  Horizontal },
        { (Direction.None, Direction.Left), Vertical },
    };
    private static readonly Dictionary<(Direction, Direction), char> bottomRightMap =
    new()
    {
        { (Direction.Bottom | Direction.Right, Direction.Bottom | Direction.Right), HorizontalAndVertical },
        { (Direction.Bottom | Direction.Right, Direction.Bottom),  HorizontalAndDown },
        { (Direction.Bottom | Direction.Right, Direction.Right), VerticalAndRight },

        { (Direction.Bottom,  Direction.Bottom | Direction.Right), VerticalAndLeft },
        { (Direction.Bottom,  Direction.Bottom),  Horizontal },
        { (Direction.Bottom,  Direction.Right), Vertical },

        { (Direction.Right, Direction.Bottom | Direction.Right), HorizontalAndUp },
        { (Direction.Right, Direction.Bottom),  Horizontal },
        { (Direction.Right, Direction.Right), Vertical },

        { (Direction.None, Direction.Bottom | Direction.Right), BottomRight },
        { (Direction.None, Direction.Bottom),  Horizontal },
        { (Direction.None, Direction.Right), Vertical },
    };

    public async Task<bool> Arrange()
    {
        bool borderUpdated = false;

        if (Dirty)
        {
            await OnArrange();
            Dirty = false;
            borderUpdated = true;
        }

        bool workDone = await SubPane.Arrange();

        return workDone || borderUpdated;
    }

    private Task OnArrange()
    {
        var width = Math.Max(SizeX - 2, 0);
        string topString = BorderSides.HasFlag(Direction.Top) ? new string(Horizontal, SizeX) : new string(' ', width);
        string bottomString = BorderSides.HasFlag(Direction.Bottom) ? new string(Horizontal, SizeX) : new string(' ', width);

        string middleString = new(' ', SizeX);
        if (middleString.Length > 0)
        {
            if (BorderSides.HasFlag(Direction.Right))
                middleString = middleString[0..^1] + Vertical;
            if (BorderSides.HasFlag(Direction.Left))
                middleString = Vertical + middleString[1..];
        }

        var topLeftKey = (BorderConnections & (Direction.Top | Direction.Left), BorderSides & (Direction.Top | Direction.Left));
        char topLeftChar = topLeftMap.TryGetValue(topLeftKey, out char foundTLChar) ? foundTLChar : '@';

        var topRightKey = (BorderConnections & (Direction.Top | Direction.Right), BorderSides & (Direction.Top | Direction.Right));
        var topRightChar = topRightMap.TryGetValue(topRightKey, out char foundTRChar) ? foundTRChar : '@';

        var bottomLeftKey = (BorderConnections & (Direction.Bottom | Direction.Left), BorderSides & (Direction.Bottom | Direction.Left));
        var bottomLeftChar = bottomLeftMap.TryGetValue(bottomLeftKey, out char foundBLChar) ? foundBLChar : '@';

        var bottomRightKey = (BorderConnections & (Direction.Bottom | Direction.Right), BorderSides & (Direction.Bottom | Direction.Right));
        var bottomRightChar = bottomRightMap.TryGetValue(bottomRightKey, out char foundBRChar) ? foundBRChar : '@';

        if (topString.Length > 1)
            topString = topLeftChar + topString[1..^1] + topRightChar;
        if (bottomString.Length > 1)
            bottomString = bottomLeftChar + bottomString[1..^1] + bottomRightChar;

        borderBuffer = new string[SizeY];
        //await TerminalWriter.LogMessage($"{SizeY}");

        if (SizeY > 0)
        {
            borderBuffer[0] = topString;
            borderBuffer[SizeY - 1] = bottomString;

            for (int i = 1; i < SizeY - 1; i++)
            {
                borderBuffer[i] = middleString;
            }
        }

        //Subpane setup
        SubPane.SizeX = SizeX;
        SubPane.SizeY = SizeY;
        SubPane.OriginX = OriginX;
        SubPane.OriginY = OriginY;

        if (BorderSides.HasFlag(Direction.Top))
        {
            SubPane.SizeY--;
            SubPane.OriginY++;
        }
        if (BorderSides.HasFlag(Direction.Right))
        {
            SubPane.SizeX--;
        }
        if (BorderSides.HasFlag(Direction.Bottom))
        {
            SubPane.SizeY--;
        }
        if (BorderSides.HasFlag(Direction.Left))
        {
            SubPane.SizeX--;
            SubPane.OriginX++;
        }

        templateCell = new SVCell
        {
            ForegroundColor = BorderColor,
            BackgroundColor = BackgroundColor
        };

        return Task.CompletedTask;
    }

    public async Task Draw(SVContext context)
    {
        for (int y = 0; y < SizeY; y++)
        {
            var borderRow = borderBuffer[y];
            for (int x = 0; x < SizeX; x++)
            {
                context[x, y] = templateCell with { Data = borderRow[x] };
            }
        }

        await SubPane.Draw(new(SubPane.OriginX, SubPane.OriginY, context.Buffer));
    }

    public async Task Invalidate()
    {
        Dirty = true;
        await SubPane.Invalidate();
    }

}
