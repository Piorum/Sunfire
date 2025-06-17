using SunfireFramework.Enums;
using SunfireFramework.Rendering;
using SunfireFramework.Terminal;

namespace SunfireFramework.Views;

public class BorderSV : IRelativeSunfireView
{
    public int X { get; set; }
    public int Y { get; set; }
    public int Z { get; set; }

    public SVFillStyle FillStyleX  { set; get; } = SVFillStyle.Max;
    public SVFillStyle FillStyleY  { set; get; } = SVFillStyle.Max;
    public int StaticX  { set; get; } = 1; //1 = 1 Cell
    public int StaticY  { set; get; } = 1; //1 = 1 Cell
    public float PercentX  { set; get; } = 1.0f; //1.0f == 100%
    public float PercentY  { set; get; } = 1.0f; //1.0f == 100%

    public int OriginX { set; get; }
    public int OriginY { set; get; }
    public int SizeX { set; get; }
    public int SizeY { set; get; }

    public SVDirection BorderSides = SVDirection.None;
    public SVDirection BorderConnections = SVDirection.None;

    public SVColor BorderColor { set; get; } = new() { R = 255, G = 255, B = 255};
    public SVColor BackgroundColor { set; get; } = new() { R = 0, G = 0, B = 0};

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
    private static readonly Dictionary<(SVDirection, SVDirection), char> topLeftMap =
    new()
    {
        { (SVDirection.Top | SVDirection.Left, SVDirection.Top | SVDirection.Left), HorizontalAndVertical },
        { (SVDirection.Top | SVDirection.Left, SVDirection.Top),  HorizontalAndUp },
        { (SVDirection.Top | SVDirection.Left, SVDirection.Left), VerticalAndLeft },

        { (SVDirection.Top, SVDirection.Top | SVDirection.Left), VerticalAndRight },
        { (SVDirection.Top, SVDirection.Top),  Horizontal },
        { (SVDirection.Top, SVDirection.Left), Vertical },

        { (SVDirection.Left, SVDirection.Top | SVDirection.Left), HorizontalAndDown },
        { (SVDirection.Left, SVDirection.Top),  Horizontal },
        { (SVDirection.Left, SVDirection.Left), Vertical },

        { (SVDirection.None, SVDirection.Top | SVDirection.Left), TopLeft },
        { (SVDirection.None, SVDirection.Top),  Horizontal },
        { (SVDirection.None, SVDirection.Left), Vertical },
    };
    private static readonly Dictionary<(SVDirection, SVDirection), char> topRightMap =
    new()
    {
        { (SVDirection.Top | SVDirection.Right, SVDirection.Top | SVDirection.Right), HorizontalAndVertical },
        { (SVDirection.Top | SVDirection.Right, SVDirection.Top),  HorizontalAndUp },
        { (SVDirection.Top | SVDirection.Right, SVDirection.Right), VerticalAndRight },

        { (SVDirection.Top,  SVDirection.Top | SVDirection.Right), VerticalAndLeft },
        { (SVDirection.Top,  SVDirection.Top),  Horizontal },
        { (SVDirection.Top,  SVDirection.Right), Vertical },

        { (SVDirection.Right, SVDirection.Top | SVDirection.Right), HorizontalAndDown },
        { (SVDirection.Right, SVDirection.Top),  Horizontal },
        { (SVDirection.Right, SVDirection.Right), Vertical },

        { (SVDirection.None, SVDirection.Top | SVDirection.Right), TopRight },
        { (SVDirection.None, SVDirection.Top),  Horizontal },
        { (SVDirection.None, SVDirection.Right), Vertical },
    };
    private static readonly Dictionary<(SVDirection, SVDirection), char> bottomLeftMap =
    new()
    {
        { (SVDirection.Bottom | SVDirection.Left, SVDirection.Bottom | SVDirection.Left), HorizontalAndVertical },
        { (SVDirection.Bottom | SVDirection.Left, SVDirection.Bottom),  HorizontalAndDown },
        { (SVDirection.Bottom | SVDirection.Left, SVDirection.Left), VerticalAndLeft },

        { (SVDirection.Bottom,  SVDirection.Bottom | SVDirection.Left), VerticalAndRight },
        { (SVDirection.Bottom,  SVDirection.Bottom),  Horizontal },
        { (SVDirection.Bottom,  SVDirection.Left), Vertical },

        { (SVDirection.Left, SVDirection.Bottom | SVDirection.Left), HorizontalAndUp },
        { (SVDirection.Left, SVDirection.Bottom),  Horizontal },
        { (SVDirection.Left, SVDirection.Left), Vertical },

        { (SVDirection.None, SVDirection.Bottom | SVDirection.Left), BottomLeft },
        { (SVDirection.None, SVDirection.Bottom),  Horizontal },
        { (SVDirection.None, SVDirection.Left), Vertical },
    };
    private static readonly Dictionary<(SVDirection, SVDirection), char> bottomRightMap =
    new()
    {
        { (SVDirection.Bottom | SVDirection.Right, SVDirection.Bottom | SVDirection.Right), HorizontalAndVertical },
        { (SVDirection.Bottom | SVDirection.Right, SVDirection.Bottom),  HorizontalAndDown },
        { (SVDirection.Bottom | SVDirection.Right, SVDirection.Right), VerticalAndRight },

        { (SVDirection.Bottom,  SVDirection.Bottom | SVDirection.Right), VerticalAndLeft },
        { (SVDirection.Bottom,  SVDirection.Bottom),  Horizontal },
        { (SVDirection.Bottom,  SVDirection.Right), Vertical },

        { (SVDirection.Right, SVDirection.Bottom | SVDirection.Right), HorizontalAndUp },
        { (SVDirection.Right, SVDirection.Bottom),  Horizontal },
        { (SVDirection.Right, SVDirection.Right), Vertical },

        { (SVDirection.None, SVDirection.Bottom | SVDirection.Right), BottomRight },
        { (SVDirection.None, SVDirection.Bottom),  Horizontal },
        { (SVDirection.None, SVDirection.Right), Vertical },
    };

    public async Task Arrange()
    {
        //Populating border buffer
        //Square
        //Rounded

        var width = Math.Max(SizeX - 2, 0);
        string topString = BorderSides.HasFlag(SVDirection.Top) ? new string(Horizontal, SizeX) : new string(' ', width);
        string bottomString = BorderSides.HasFlag(SVDirection.Bottom) ? new string(Horizontal, SizeX) : new string(' ', width);

        string middleString = new(' ', SizeX);
        if (middleString.Length > 0)
        {
            if (BorderSides.HasFlag(SVDirection.Right))
                middleString = middleString[0..^1] + Vertical;
            if (BorderSides.HasFlag(SVDirection.Left))
                middleString = Vertical + middleString[1..];
        }

        var topLeftKey = (BorderConnections & (SVDirection.Top | SVDirection.Left), BorderSides & (SVDirection.Top | SVDirection.Left));
        char topLeftChar = topLeftMap.TryGetValue(topLeftKey, out char foundTLChar) ? foundTLChar : '@';

        var topRightKey = (BorderConnections & (SVDirection.Top | SVDirection.Right), BorderSides & (SVDirection.Top | SVDirection.Right));
        var topRightChar = topRightMap.TryGetValue(topRightKey, out char foundTRChar) ? foundTRChar : '@';

        var bottomLeftKey = (BorderConnections & (SVDirection.Bottom | SVDirection.Left), BorderSides & (SVDirection.Bottom | SVDirection.Left));
        var bottomLeftChar = bottomLeftMap.TryGetValue(bottomLeftKey, out char foundBLChar) ? foundBLChar : '@';

        var bottomRightKey = (BorderConnections & (SVDirection.Bottom | SVDirection.Right), BorderSides & (SVDirection.Bottom | SVDirection.Right));
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

        if (BorderSides.HasFlag(SVDirection.Top))
        {
            SubPane.SizeY--;
            SubPane.OriginY++;
        }
        if (BorderSides.HasFlag(SVDirection.Right))
        {
            SubPane.SizeX--;
        }
        if (BorderSides.HasFlag(SVDirection.Bottom))
        {
            SubPane.SizeY--;
        }
        if (BorderSides.HasFlag(SVDirection.Left))
        {
            SubPane.SizeX--;
            SubPane.OriginX++;
        }

        templateCell = new SVCell
        {
            ForegroundColor = BorderColor,
            BackgroundColor = BackgroundColor
        };

        await SubPane.Arrange();
    }

    public async Task Draw(SVContext context)
    {
        for (int y = 0; y < SizeY; y++)
        {
            var borderRow = borderBuffer[y];
            for (int x = 0; x < SizeX; x++)
            {
                context[x, y] = templateCell with { Char = borderRow[x] };
            }
        }

        await SubPane.Draw(new(SubPane.OriginX, SubPane.OriginY, context.Buffer));
    }

}
