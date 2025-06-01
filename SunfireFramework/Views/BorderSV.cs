
using SunfireFramework.Enums;

namespace SunfireFramework.Views;

public class BorderSV : IRelativeSunfireView
{
    public int X { get; set; }
    public int Y { get; set; }
    public int Z { get; set; }

    public int OriginX { set; get; }
    public int OriginY { set; get; }
    public int SizeX { set; get; }
    public int SizeY { set; get; }

    public SVBorderStyle SVBorderStyle = SVBorderStyle.None;

    public ConsoleColor BorderColor = ConsoleColor.White;
    public ConsoleColor BackgroundColor { get; set; } = ConsoleColor.Black;

    required public PaneSV SubPane { set; get; }

    public Task Arrange()
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

        return Task.CompletedTask;
    }

    public Task Draw()
    {
        //Draw border here
        return Task.CompletedTask;
    }
}
