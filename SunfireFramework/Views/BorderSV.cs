using SunfireFramework.Enums;
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

    public SVBorderStyle BorderStyle = SVBorderStyle.None;
    public SVBorderConnection BorderConnections = SVBorderConnection.None;

    public ConsoleColor BorderColor = ConsoleColor.White;
    public ConsoleColor BackgroundColor { get; set; } = ConsoleColor.Black;

    required public PaneSV SubPane { set; get; }

    //Border buffer
    private string[] borderBuffer = [];
    
    //Box building characters
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
    //Connections, Borderstyle, ProperCornerChar
    private static readonly Dictionary<(SVBorderConnection, SVBorderStyle), char> topLeftMap =
    new()
    {
        { (SVBorderConnection.Top | SVBorderConnection.Left, SVBorderStyle.Top | SVBorderStyle.Left), HorizontalAndVertical },
        { (SVBorderConnection.Top | SVBorderConnection.Left, SVBorderStyle.Top),  HorizontalAndUp },
        { (SVBorderConnection.Top | SVBorderConnection.Left, SVBorderStyle.Left), VerticalAndLeft },

        { (SVBorderConnection.Top,  SVBorderStyle.Top | SVBorderStyle.Left), VerticalAndRight },
        { (SVBorderConnection.Top,  SVBorderStyle.Top),  Horizontal },
        { (SVBorderConnection.Top,  SVBorderStyle.Left), Vertical },

        { (SVBorderConnection.Left, SVBorderStyle.Top | SVBorderStyle.Left), HorizontalAndDown },
        { (SVBorderConnection.Left, SVBorderStyle.Top),  Horizontal },
        { (SVBorderConnection.Left, SVBorderStyle.Left), Vertical },

        { (SVBorderConnection.None, SVBorderStyle.Top | SVBorderStyle.Left), TopLeft },
        { (SVBorderConnection.None, SVBorderStyle.Top),  Horizontal },
        { (SVBorderConnection.None, SVBorderStyle.Left), Vertical },
    };
    private static readonly Dictionary<(SVBorderConnection, SVBorderStyle), char> topRightMap =
    new()
    {
        { (SVBorderConnection.Top | SVBorderConnection.Right, SVBorderStyle.Top | SVBorderStyle.Right), HorizontalAndVertical },
        { (SVBorderConnection.Top | SVBorderConnection.Right, SVBorderStyle.Top),  HorizontalAndUp },
        { (SVBorderConnection.Top | SVBorderConnection.Right, SVBorderStyle.Right), VerticalAndRight },

        { (SVBorderConnection.Top,  SVBorderStyle.Top | SVBorderStyle.Right), VerticalAndLeft },
        { (SVBorderConnection.Top,  SVBorderStyle.Top),  Horizontal },
        { (SVBorderConnection.Top,  SVBorderStyle.Right), Vertical },

        { (SVBorderConnection.Right, SVBorderStyle.Top | SVBorderStyle.Right), HorizontalAndDown },
        { (SVBorderConnection.Right, SVBorderStyle.Top),  Horizontal },
        { (SVBorderConnection.Right, SVBorderStyle.Right), Vertical },

        { (SVBorderConnection.None, SVBorderStyle.Top | SVBorderStyle.Right), TopRight },
        { (SVBorderConnection.None, SVBorderStyle.Top),  Horizontal },
        { (SVBorderConnection.None, SVBorderStyle.Right), Vertical },
    };
    private static readonly Dictionary<(SVBorderConnection, SVBorderStyle), char> bottomLeftMap =
    new()
    {
        { (SVBorderConnection.Bottom | SVBorderConnection.Left, SVBorderStyle.Bottom | SVBorderStyle.Left), HorizontalAndVertical },
        { (SVBorderConnection.Bottom | SVBorderConnection.Left, SVBorderStyle.Bottom),  HorizontalAndDown },
        { (SVBorderConnection.Bottom | SVBorderConnection.Left, SVBorderStyle.Left), VerticalAndLeft },

        { (SVBorderConnection.Bottom,  SVBorderStyle.Bottom | SVBorderStyle.Left), VerticalAndRight },
        { (SVBorderConnection.Bottom,  SVBorderStyle.Bottom),  Horizontal },
        { (SVBorderConnection.Bottom,  SVBorderStyle.Left), Vertical },

        { (SVBorderConnection.Left, SVBorderStyle.Bottom | SVBorderStyle.Left), HorizontalAndUp },
        { (SVBorderConnection.Left, SVBorderStyle.Bottom),  Horizontal },
        { (SVBorderConnection.Left, SVBorderStyle.Left), Vertical },

        { (SVBorderConnection.None, SVBorderStyle.Bottom | SVBorderStyle.Left), BottomLeft },
        { (SVBorderConnection.None, SVBorderStyle.Bottom),  Horizontal },
        { (SVBorderConnection.None, SVBorderStyle.Left), Vertical },
    };
    private static readonly Dictionary<(SVBorderConnection, SVBorderStyle), char> bottomRightMap =
    new()
    {
        { (SVBorderConnection.Bottom | SVBorderConnection.Right, SVBorderStyle.Bottom | SVBorderStyle.Right), HorizontalAndVertical },
        { (SVBorderConnection.Bottom | SVBorderConnection.Right, SVBorderStyle.Bottom),  HorizontalAndDown },
        { (SVBorderConnection.Bottom | SVBorderConnection.Right, SVBorderStyle.Right), VerticalAndRight },

        { (SVBorderConnection.Bottom,  SVBorderStyle.Bottom | SVBorderStyle.Right), VerticalAndLeft },
        { (SVBorderConnection.Bottom,  SVBorderStyle.Bottom),  Horizontal },
        { (SVBorderConnection.Bottom,  SVBorderStyle.Right), Vertical },

        { (SVBorderConnection.Right, SVBorderStyle.Bottom | SVBorderStyle.Right), HorizontalAndUp },
        { (SVBorderConnection.Right, SVBorderStyle.Bottom),  Horizontal },
        { (SVBorderConnection.Right, SVBorderStyle.Right), Vertical },

        { (SVBorderConnection.None, SVBorderStyle.Bottom | SVBorderStyle.Right), BottomRight },
        { (SVBorderConnection.None, SVBorderStyle.Bottom),  Horizontal },
        { (SVBorderConnection.None, SVBorderStyle.Right), Vertical },
    };

    public async Task Arrange()
    {
        //Populating border buffer
        //Square
        //const char TopLeft = (char)9484;
        //const char TopRight = (char)9488;
        //const char BottomLeft = (char)9492;
        //const char BottomRight = (char)9496;
        //const char TopLeft = (char)9484;
        //Rounded

        var width = Math.Max(SizeX - 2, 0);
        string topString = BorderStyle.HasFlag(SVBorderStyle.Top) ? new string(Horizontal, SizeX) : new string(' ', width);
        string bottomString = BorderStyle.HasFlag(SVBorderStyle.Bottom) ? new string(Horizontal, SizeX) : new string(' ', width);

        string middleString = new string(' ', SizeX);
        if (middleString.Length > 0)
        {
            if (BorderStyle.HasFlag(SVBorderStyle.Right))
                middleString = middleString[0..^1] + Vertical;
            if (BorderStyle.HasFlag(SVBorderStyle.Left))
                middleString = Vertical + middleString[1..];
        }

        var topLeftKey = (BorderConnections & (SVBorderConnection.Top | SVBorderConnection.Left), BorderStyle & (SVBorderStyle.Top | SVBorderStyle.Left));
        char topLeftChar = topLeftMap.TryGetValue(topLeftKey, out char foundTLChar) ? foundTLChar : '@';

        var topRightKey = (BorderConnections & (SVBorderConnection.Top | SVBorderConnection.Right), BorderStyle & (SVBorderStyle.Top | SVBorderStyle.Right));
        var topRightChar = topRightMap.TryGetValue(topRightKey, out char foundTRChar) ? foundTRChar : '@';

        var bottomLeftKey = (BorderConnections & (SVBorderConnection.Bottom | SVBorderConnection.Left), BorderStyle & (SVBorderStyle.Bottom | SVBorderStyle.Left));
        var bottomLeftChar = bottomLeftMap.TryGetValue(bottomLeftKey, out char foundBLChar) ? foundBLChar : '@';

        var bottomRightKey = (BorderConnections & (SVBorderConnection.Bottom | SVBorderConnection.Right), BorderStyle & (SVBorderStyle.Bottom | SVBorderStyle.Right));
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

        if (BorderStyle.HasFlag(SVBorderStyle.Top))
        {
            SubPane.SizeY--;
            SubPane.OriginY++;
        }
        if (BorderStyle.HasFlag(SVBorderStyle.Right))
        {
            SubPane.SizeX--;
        }
        if (BorderStyle.HasFlag(SVBorderStyle.Bottom))
        {
            SubPane.SizeY--;
        }
        if (BorderStyle.HasFlag(SVBorderStyle.Left))
        {
            SubPane.SizeX--;
            SubPane.OriginX++;
        }

        await SubPane.Arrange();
    }

    public async Task Draw()
    {
        List<TerminalOutput> outputs = [];
        for (int i = 0; i < borderBuffer.Length; i++)
        {
            outputs.Add(new()
            {
                X = OriginX,
                Y = OriginY + i,
                Output = borderBuffer[i]
            });
        }
        await TerminalWriter.WriteAsync(outputs, foregroundColor: BorderColor, backgroundColor: BackgroundColor);

        await SubPane.Draw();
    }

}
