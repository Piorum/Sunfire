using System.Numerics;
using Sunfire.Enums;

namespace Sunfire.Views;

public class View
{
    required public int X;
    required public int Y;

    public int OriginX; // Top Left
    public int OriginY; // Top Left
    public int SizeX; // Width
    public int SizeY; // Height

    required public FillStyle FillStyleWidth;
    required public FillStyle FillStyleHeight;
    public float WidthPercent = 1.0f; //1.0f == 100%
    public float HeightPercent = 1.0f; //1.0f == 100%

    public BorderStyle BorderStyle = BorderStyle.None;

    public List<View> SubViews { get; } = [];
    public View? Container { get; internal set; } = null;

    public Task AddAsync(View subView)
    {
        subView.Container = this;
        SubViews.Add(subView);
        return Task.CompletedTask;
    }

    public async Task Arrange(int WidthConstraint, int HeightConstraint)
    {
        SizeX = WidthConstraint;
        SizeY = HeightConstraint;

        //Console.WriteLine($"WidthConstraint {WidthConstraint}, HeightConstraint {HeightConstraint}");

        var xLevels = SubViews.Select(v => v.X).Distinct();
        var yLevels = SubViews.Select(v => v.Y).Distinct();

        Dictionary<int, int> availableWidth = [];
        Dictionary<int, int> availableHeight = [];
        foreach (var level in yLevels)
        {
            availableWidth[level] = SizeX;
        }
        foreach (var level in xLevels)
        {
            availableHeight[level] = SizeY;
        }

        //Width
        var minViewsW = SubViews.Where(v => v.FillStyleWidth == FillStyle.Min);
        var perViewsW = SubViews.Where(v => v.FillStyleWidth == FillStyle.Percent);
        var fillViewsW = SubViews.Where(v => v.FillStyleWidth == FillStyle.Max).ToList();

        foreach (var view in minViewsW)
        {
            view.SizeX = 1;
            availableWidth[view.Y]--;
        }
        foreach (var view in perViewsW)
        {
            view.SizeX = (int)(availableWidth[view.Y] * view.WidthPercent);
            availableWidth[view.Y] -= view.SizeX;
        }
        foreach (var view in fillViewsW)
        {
            view.SizeX = availableWidth[view.Y];
        }

        //Height
        var minViewsH = SubViews.Where(v => v.FillStyleHeight == FillStyle.Min);
        var perViewsH = SubViews.Where(v => v.FillStyleHeight == FillStyle.Percent);
        var fillViewsH = SubViews.Where(v => v.FillStyleHeight == FillStyle.Max).ToList();

        foreach (var view in minViewsH)
        {
            view.SizeY = 1;
            foreach (var xLevel in xLevels)
            {
                availableHeight[xLevel]--;
            }
        }
        foreach (var view in perViewsH)
        {
            view.SizeY = (int)(availableHeight[view.X] * view.HeightPercent);
            foreach (var xLevel in xLevels)
            {
                availableHeight[xLevel] -= view.SizeY;
            }
        }
        foreach (var view in fillViewsH)
        {
            view.SizeY = availableHeight[view.X];
        }

        //Positioning
        int CursorPosX = OriginX;
        int CursorPosY = OriginY;
        foreach (var yLevel in yLevels)
        {
            int largestY = 0;
            foreach (var xLevel in xLevels)
            {
                View? view = SubViews.Where(v => v.X == xLevel && v.Y == yLevel).FirstOrDefault();
                if (view is null) continue;

                view.OriginX = CursorPosX;
                view.OriginY = CursorPosY;

                CursorPosX += view.SizeX;

                if (view.SizeY > largestY) largestY = view.SizeY;
            }
            CursorPosX = OriginX;
            CursorPosY += largestY;
        }

        List<Task> arrangeTasks = [];
        foreach (var view in SubViews)
        {
            arrangeTasks.Add(view.Arrange(view.SizeX, view.SizeY));
        }
        await Task.WhenAll(arrangeTasks);

        await Draw();
    }

    public async Task Draw()
    {
        //await Console.Out.WriteLineAsync($"Origin: ({OriginX},{OriginY}), Size: <{SizeX},{SizeY}>");

        var value = 0;
        foreach (var view in SubViews)
        {
            Console.BackgroundColor = value switch
            {
                0 => ConsoleColor.White,
                1 => ConsoleColor.Red,
                2 => ConsoleColor.Green,
                3 => ConsoleColor.Blue,
                4 => ConsoleColor.Yellow,
                _ => ConsoleColor.Black
            };
            for (int i = 0; i < view.SizeY; i++)
            {
                var index = i;
                Console.SetCursorPosition(view.OriginX, view.OriginY + index);
                Console.Write(new string(' ', view.SizeX));
            }
            value++;
        }

        List<Task> drawTasks = [];
        foreach (var view in SubViews)
        {
            drawTasks.Add(view.Draw());
        }
        await Task.WhenAll(drawTasks);
    }
}
