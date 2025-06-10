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
        var orderedByWidthStyle = SubViews.OrderBy(sv => sv.FillStyleX).ToList();
        var orderedByHeightStyle = SubViews.OrderBy(sv => sv.FillStyleY).ToList();

        List<Task> measureTasks = [];
        foreach (var zLevel in zLevels!)
        {
            var zIndex = zLevel;
            measureTasks.Add(Task.Run(async () =>
            {
                //Border Connection Task
                var connectionsTask = Task.Run(() =>
                {
                    var borderViews = SubViews.OfType<BorderSV>().ToList();
                    var viewCoordinates = borderViews.Select(v => (v.X, v.Y)).ToHashSet();

                    foreach (var view in borderViews)
                    {
                        if (viewCoordinates.Contains((view.X, view.Y - 1)))
                        {
                            view.BorderConnections |= Direction.Top;
                        }
                        if (viewCoordinates.Contains((view.X, view.Y + 1)))
                        {
                            view.BorderConnections |= Direction.Bottom;
                        }
                        if (viewCoordinates.Contains((view.X - 1, view.Y)))
                        {
                            view.BorderConnections |= Direction.Left;
                        }
                        if (viewCoordinates.Contains((view.X + 1, view.Y)))
                        {
                            view.BorderConnections |= Direction.Right;
                        }
                    }
                });
                
                //Width
                var widthTask = Task.Run(() =>
                {
                    int[] availableWidth = new int[yLevels!.Count];
                    Array.Fill(availableWidth, SizeX);
                    foreach (var view in orderedByWidthStyle)
                    {
                        if (view.Z != zIndex) continue;

                        switch (view.FillStyleX)
                        {
                            case SVFillStyle.Static:
                                view.SizeX = Math.Min(availableWidth[view.Y], view.StaticX);
                                break;
                            case SVFillStyle.Min:
                                view.SizeX = availableWidth[view.Y] > 0 ? 1 : 0;
                                break;
                            case SVFillStyle.Percent:
                                view.SizeX = (int)(availableWidth[view.Y] * view.PercentX);
                                break;
                            case SVFillStyle.Max:
                                view.SizeX = availableWidth[view.Y];
                                break;
                        }
                        if (view.FillStyleX != SVFillStyle.Max)
                            availableWidth[view.Y] -= view.SizeX;
                    }
                });

                //Height
                var heightTask = Task.Run(() =>
                {
                    int[] availableHeight = new int[xLevels!.Count];
                    Array.Fill(availableHeight, SizeY);

                    int[] largestAtY = new int[yLevels!.Count];

                    foreach (var view in orderedByHeightStyle)
                    {
                        if (view.Z != zIndex) continue;

                        switch (view.FillStyleY)
                        {
                            case SVFillStyle.Static:
                                view.SizeY = Math.Min(availableHeight[view.X], view.StaticY);
                                break;
                            case SVFillStyle.Min:
                                view.SizeY = availableHeight[view.X] > 0 ? 1 : 0;
                                break;
                            case SVFillStyle.Percent:
                                view.SizeY = (int)(availableHeight[view.X] * view.PercentY);
                                break;
                            case SVFillStyle.Max:
                                view.SizeY = availableHeight[view.X];
                                break;
                        }

                        if (view.SizeY > largestAtY[view.Y])
                        {
                            if (view.FillStyleY != SVFillStyle.Max)
                                for (int i = 0; i < availableHeight.Length; i++)
                                    availableHeight[i] -= view.SizeY - largestAtY[view.Y];
                            largestAtY[view.Y] = view.SizeY;
                        }
                    }
                });

                await Task.WhenAll(connectionsTask, widthTask, heightTask);
            }));
        }
        await Task.WhenAll(measureTasks);

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

        await Task.WhenAll(SubViews.Select(v => v.Arrange()));
    }

    public async Task Draw()
    {
        if (zLevels!.Count == 1)
        {
            await Task.WhenAll(SubViews.Select(v => v.Draw()));

        }
        else
        {
            Parallel.ForEach(zLevels, async zLevel =>
            {
                await Task.WhenAll(SubViews.Where(sv => sv.Z == zLevel).Select(v => v.Draw()));
            });
        }
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
}
