
using SunfireFramework.Enums;

namespace SunfireFramework.Views;

public class PaneSV : IRelativeSunfireView
{
    public int X { get; set; }
    public int Y { get; set; }
    public int Z { get; set; }

    public int OriginX { set; get; }
    public int OriginY { set; get; }
    public int SizeX { set; get; }
    public int SizeY { set; get; }

    public SVFillStyle FillStyleX = SVFillStyle.Max;
    public SVFillStyle FillStyleY = SVFillStyle.Max;
    public int StaticX = 1; //1 = 1 Cell
    public int StaticY = 1; //1 = 1 Cell
    public float PercentX = 1.0f; //1.0f == 100%
    public float PercentY = 1.0f; //1.0f == 100%

    public ConsoleColor BackgroundColor { get; set; } = ConsoleColor.Black;
    
    required public List<IRelativeSunfireView> SubViews = [];

    public async Task Arrange()
    {
        //Arrange Subviews
        //Measure, Position

        var test = SubViews.First();
        test.OriginX = OriginX;
        test.OriginY = OriginY;
        test.SizeX = SizeX;
        test.SizeY = SizeY;

        await Task.WhenAll(SubViews.Select(v => v.Arrange()));
    }

    public async Task Draw()
    {
        await Task.WhenAll(SubViews.Select(v => v.Draw()));
    }
}
