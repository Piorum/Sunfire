
using SunfireFramework.Enums;

namespace SunfireFramework.Views;

public class PaneSV : IRelativeSunfireView
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

    public ConsoleColor BackgroundColor { get; set; } = ConsoleColor.Black;

    required public List<IRelativeSunfireView> SubViews = [];

    protected List<int>? xLevels;
    protected List<int>? yLevels;
    protected List<int>? zLevels;

    public async Task Arrange()
    {
        await PopulateXYZLevels();

        //Measurement
        foreach (var zLevel in zLevels!)
        {
            int baseWidth = SizeX;
            int baseHeight = SizeY;

            Dictionary<int, int> availableWidth = yLevels!.ToDictionary(y => y, _ => baseWidth);
            Dictionary<int, int> availableHeight = xLevels!.ToDictionary(x => x, _ => baseHeight);

            //Width
            var orderByWidthStyle = SubViews.OrderBy(sv => (int)sv.FillStyleX).ToList();

            foreach (var view in orderByWidthStyle)
            {
                switch (view.FillStyleX)
                {
                    case SVFillStyle.Static:
                        var sizeStatic = Math.Min(availableWidth[view.Y], StaticX);
                        view.SizeX = sizeStatic;
                        availableWidth[view.Y] -= sizeStatic;
                        break;
                    case SVFillStyle.Min:
                        var sizeMin = Math.Min(availableWidth[view.Y], 1);
                        view.SizeX = sizeMin;
                        availableWidth[view.Y] -= sizeMin;
                        break;
                    case SVFillStyle.Percent:
                        view.SizeX = (int)(availableWidth[view.Y] * view.PercentX);
                        availableWidth[view.Y] -= view.SizeX;
                        break;
                    case SVFillStyle.Max:
                        view.SizeX = availableWidth[view.Y];
                        break;
                }
            }

            //Height
            var orderByHeightStyle = SubViews.OrderBy(sv => sv.FillStyleY).ToList();

            foreach (var view in orderByHeightStyle)
            {
                switch (view.FillStyleY)
                {
                    case SVFillStyle.Static:
                        var sizeStatic = Math.Min(availableHeight[view.X], StaticY);
                        view.SizeY = sizeStatic;
                        foreach (var xLevel in xLevels!)
                        {
                            availableHeight[xLevel] -= sizeStatic;
                        }
                        break;
                    case SVFillStyle.Min:
                        var sizeMin = Math.Min(availableHeight[view.X], 1);
                        view.SizeY = sizeMin;
                        foreach (var xLevel in xLevels!)
                        {
                            availableHeight[xLevel] -= sizeMin;
                        }
                        break;
                    case SVFillStyle.Percent:
                        view.SizeY = (int)(availableHeight[view.X] * view.PercentY);
                        foreach (var xLevel in xLevels!)
                        {
                            availableHeight[xLevel] -= view.SizeY;
                        }
                        break;
                    case SVFillStyle.Max:
                        view.SizeY = availableHeight[view.X];
                        break;
                }
            }
        }

        //Positioning
        var subViewGrid = SubViews.ToLookup(sv => (sv.X, sv.Y, sv.Z));

        foreach (var zLevel in zLevels!)
        {
            var CursorX = OriginX;
            var CursorY = OriginY;
            foreach (var yLevel in yLevels!)
            {
                var largestY = 0;
                foreach (var xLevel in xLevels!)
                {
                    IRelativeSunfireView? subView = subViewGrid[(xLevel, yLevel, zLevel)].FirstOrDefault();
                    if (subView is null) continue;

                    subView.OriginX = CursorX;
                    subView.OriginY = CursorY;

                    CursorX += subView.SizeX;

                    largestY = subView.SizeY > largestY ? subView.SizeY : largestY;
                }
                CursorX = OriginX;
                CursorY += largestY;
            }
        }
        //Order by z so they are drawn in the right order
        SubViews = [.. SubViews.OrderBy(sv => sv.Z)];

        await Task.WhenAll(SubViews.Select(v => v.Arrange()));
    }

    public async Task Draw()
    {
        await Task.WhenAll(SubViews.Select(v => v.Draw()));
    }

    protected Task PopulateXYZLevels()
    {
        HashSet<int> xSet = [];
        HashSet<int> ySet = [];
        HashSet<int> zSet = [];

        foreach (var view in SubViews)
        {
            xSet.Add(view.X);
            ySet.Add(view.Y);
            zSet.Add(view.Z);
        }

        xLevels = [.. xSet.Order()];
        yLevels = [.. ySet.Order()];
        zLevels = [.. zSet.Order()];

        return Task.CompletedTask;
    }

    protected class SVSize
    {
        public int Width;
        public int Height;
    }
}
