using Sunfire.Tui.Enums;
using Sunfire.Tui.Interfaces;
using Sunfire.Tui.Models;

namespace Sunfire.Views;

public class InfosView : IRelativeSunfireView
{
    public int X { get; set; }
    public int Y { get; set; }
    public int Z { get; set; }

    public FillStyle FillStyleX { set; get; } = FillStyle.Max;
    public FillStyle FillStyleY { set; get; } = FillStyle.Min;
    public int StaticX { set; get; } = 1; //1 = 1 Cell
    public int StaticY { set; get; } = 1; //1 = 1 Cell
    public float PercentX { set; get; } = 1.0f; //1.0f == 100%
    public float PercentY { set; get; } = 1.0f; //1.0f == 100%

    public int MinX { set; get; } = 3;
    public int MinY => SubViews.Count * 3;

    public int OriginX { set; get; }
    public int OriginY { set; get; }
    public int SizeX { set; get; }
    public int SizeY { set; get; }

    public readonly List<InfoView> SubViews = [];

    private bool Dirty;

    public async Task<bool> Arrange()
    {
        if(!Dirty)
            return false;

        var i = 0;
        foreach(var view in SubViews)
        {
            var newOrigin = OriginY + (i * 3);
            i++;

            if(newOrigin + 3 > OriginY + SizeY)
                continue;

            (view.OriginX, view.OriginY, view.SizeX, view.SizeY) = (OriginX, newOrigin, SizeX, 3);
        }

        await Task.WhenAll(SubViews.Select(v => v.Arrange()));

        return true;
    }    

    public async Task Draw(SVContext context) =>
        await Task.WhenAll(SubViews.Select(v => v.Draw(context)));

    public async Task Invalidate()
    {
        Dirty = true;

        await Task.WhenAll(SubViews.Select(v => v.Invalidate()));
    }
}
