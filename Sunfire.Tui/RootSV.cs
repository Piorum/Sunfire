using Sunfire.Tui.Models;
using Sunfire.Tui.Interfaces;

namespace Sunfire.Tui;

/// <summary>
/// Root view that all arranges, draws, and invalidations will initially propogate from.
/// </summary>
/// <param name="sizeX">Requested Width or null for Console.BufferWidth</param>
/// <param name="sizeY">Requested Height or null for Console.BufferHeight</param>
public class RootSV(int? sizeX = null, int? sizeY = null) : ISunfireView
{
    public int OriginX { set; get; } = 0;
    public int OriginY { set; get; } = 0;
    public int SizeX { set; get; } = sizeX ?? Console.BufferWidth;
    public int SizeY { set; get; } = sizeY ?? Console.BufferHeight;

    public bool Dirty { set; get; }

    required public ISunfireView RootView;

    public async Task<bool> Arrange()
    {
        if (Dirty)
        {
            await OnArrange();
            Dirty = false;
        }

        var workDone = await RootView.Arrange();

        return workDone;
    }

    private Task OnArrange()
    {
        RootView.OriginX = 0;
        RootView.OriginY = 0;

        RootView.SizeX = SizeX;
        RootView.SizeY = SizeY;

        return Task.CompletedTask;
    }

    public async Task Draw(SVContext context) =>
        await RootView.Draw(context);

    public async Task Invalidate()
    {
        Dirty = true;
        await RootView.Invalidate();
    }
}
