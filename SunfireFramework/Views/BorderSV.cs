using SunfireFramework.Enums;

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

    public SVBorderStyle SVBorderStyle = SVBorderStyle.None;

    public ConsoleColor BorderColor = ConsoleColor.White;
    public ConsoleColor BackgroundColor { get; set; } = ConsoleColor.Black;

    required public PaneSV SubPane { set; get; }

    public async Task Arrange()
    {
        SubPane.SizeX = SVBorderStyle switch
        {
            SVBorderStyle.Full => SizeX - 2,
            SVBorderStyle.Right or SVBorderStyle.Left => SizeX - 1,
            _ => SizeX
        };
        SubPane.SizeY = SVBorderStyle switch
        {
            SVBorderStyle.Full => SizeY - 2,
            SVBorderStyle.Top or SVBorderStyle.Bottom => SizeY - 1,
            _ => SizeY
        };

        SubPane.OriginX = SVBorderStyle switch
        {
            SVBorderStyle.Full or SVBorderStyle.Left => OriginX + 1,
            _ => OriginX
        };
        SubPane.OriginY = SVBorderStyle switch
        {
            SVBorderStyle.Full or SVBorderStyle.Top => OriginY + 1,
            _ => OriginY
        };

        await SubPane.Arrange();
    }

    public async Task Draw()
    {
        const char TopLeft = (char)9484;
        const char TopRight = (char)9488;
        const char BottomLeft = (char)9492;
        const char BottomRight = (char)9496;
        const char Horizontal = (char)9472;
        const char Vertical = (char)9474;

        string[] border = new string[SizeY];
        switch (SVBorderStyle)
        {
            case SVBorderStyle.Full:
                border[0] = $"{TopLeft}{new string(Horizontal, SizeX - 2)}{TopRight}";
                border[SizeY - 1] = $"{BottomLeft}{new string(Horizontal, SizeX - 2)}{BottomRight}";
                for (int i = 1; i < SizeY - 1; i++)
                {
                    border[i] = $"{Vertical}{new string(' ', SizeX - 2)}{Vertical}";
                }
                break;

            case SVBorderStyle.Top:
                border[0] = new string(Horizontal, SizeX);
                break;

            case SVBorderStyle.Bottom:
                border[SizeY - 1] = new string(Horizontal, SizeX);
                break;

            case SVBorderStyle.Left:
                for (int i = 0; i < SizeY; i++)
                {
                    border[i] = $"{Vertical}{new string(' ', SizeX - 1)}";
                }
                break;

            case SVBorderStyle.Right:
                for (int i = 0; i < SizeY; i++)
                {
                    border[i] = $"{new string(' ', SizeX - 1)}{Vertical}";
                }
                break;
        }

        List<ConsoleOutput> outputs = [];
        for (int i = 0; i < SizeY; i++)
        {
            outputs.Add(new()
            {
                X = OriginX,
                Y = OriginY + i,
                Output = border[i]
            });
        }
        await ConsoleWriter.WriteAsync(outputs, foregroundColor: BorderColor, backgroundColor: BackgroundColor);

        await SubPane.Draw();
    }

}
