using Sunfire.Tui.Models;

namespace Sunfire.Tui.Views;

public class RootSV(int? sizeX = null, int? sizeY = null) : ISunfireView
{
    public int OriginX { set; get; } = 0;
    public int OriginY { set; get; } = 0;
    public int SizeX { set; get; } = sizeX ?? Console.BufferWidth;
    public int SizeY { set; get; } = sizeY ?? Console.BufferHeight;

    public bool Dirty { set; get; }

    required public PaneSV RootPane;

    public async Task<bool> Arrange()
    {
        if (Dirty)
        {
            await OnArrange();
            Dirty = false;
        }

        var workDone = await RootPane.Arrange();

        return workDone;
    }

    private Task OnArrange()
    {
        RootPane.OriginX = 0;
        RootPane.OriginY = 0;

        RootPane.SizeX = SizeX;
        RootPane.SizeY = SizeY;

        return Task.CompletedTask;
    }

    public async Task Draw(SVContext context) =>
        await RootPane.Draw(context);

    public async Task Invalidate()
    {
        Dirty = true;
        await RootPane.Invalidate();
    }
}
