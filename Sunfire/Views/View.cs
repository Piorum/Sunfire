using Sunfire.Enums;

namespace Sunfire.Views;

public class View
{
    public string Tag = "";

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
    public ConsoleColor BackgroundColor = ConsoleColor.Black;

    public List<View> SubViews { get; } = [];
    public View? Container { get; internal set; } = null;

    public virtual void Add(View subView)
    {
        subView.Container = this;
        SubViews.Add(subView);
    }

    public async virtual Task Arrange(int WidthConstraint, int HeightConstraint)
    {
        await Measure(WidthConstraint, HeightConstraint);
        await Position();
        await Draw();

        List<Task> arrangeTasks = [];
        foreach (var view in SubViews)
        {
            arrangeTasks.Add(view.Arrange(view.SizeX, view.SizeY));
        }
        await Task.WhenAll(arrangeTasks);
    }

    protected virtual Task Measure(int WidthConstraint, int HeightConstraint)
    {
        SizeX = WidthConstraint;
        SizeY = HeightConstraint;

        var xLevels = SubViews.Select(v => v.X).Distinct().Order();
        var yLevels = SubViews.Select(v => v.Y).Distinct().Order();

        Dictionary<int, int> availableWidth = [];
        Dictionary<int, int> availableHeight = [];
        foreach (var level in yLevels)
        {
            availableWidth[level] = SizeX;

            switch (BorderStyle)
            {
                case BorderStyle.Full:
                    availableWidth[level] -= 2;
                    break;
                case BorderStyle.Left:
                case BorderStyle.Right:
                    availableWidth[level]--;
                    break;
            }
        }
        foreach (var level in xLevels)
        {
            availableHeight[level] = SizeY;

            switch (BorderStyle)
            {
                case BorderStyle.Full:
                    availableHeight[level] -= 2;
                    break;
                case BorderStyle.Top:
                case BorderStyle.Bottom:
                    availableHeight[level]--;
                    break;
            }
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

        return Task.CompletedTask;
    }

    protected virtual Task Position()
    {
        var xLevels = SubViews.Select(v => v.X).Distinct().Order();
        var yLevels = SubViews.Select(v => v.Y).Distinct().Order();

        //Positioning
        int StartCursorPosX = OriginX;
        int StartCursorPosY = OriginY;

        switch (BorderStyle)
        {
            case BorderStyle.Full:
                StartCursorPosX++;
                StartCursorPosY++;
                break;
            case BorderStyle.Top:
                StartCursorPosY++;
                break;
            case BorderStyle.Left:
                StartCursorPosX++;
                break;
        }

        int CursorPosX = StartCursorPosX;
        int CursorPosY = StartCursorPosY;

        //var orderedViews = SubViews.OrderBy(sv => sv.X).OrderBy(sv => sv.Y).ToList();
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
            CursorPosX = StartCursorPosX;
            CursorPosY += largestY;
        }

        return Task.CompletedTask;
    }

    public async virtual Task Draw()
    {
        //await Console.Out.WriteLineAsync($"Origin: ({OriginX},{OriginY}), Size: <{SizeX},{SizeY}>");

        Console.SetCursorPosition(OriginX, OriginY);

        Console.BackgroundColor = BackgroundColor;
        for (int i = 0; i < SizeY; i++)
        {
            Console.SetCursorPosition(OriginX, OriginY + i);

            var output = await BuildLineOutput(new string(' ', SizeX), i);

            await Console.Out.WriteAsync(output);
        }
    }

    private Task<string> BuildLineOutput(string input, int index)
    {
        string output = input;
        switch (BorderStyle)
        {
            case BorderStyle.Full:
                if (index == 0)
                {
                    output = (char)9484 + new string((char)9472, SizeX - 2) + (char)9488;
                }
                else if (index == SizeY - 1)
                {
                    output = (char)9492 + new string((char)9472, SizeX - 2) + (char)9496;
                }
                else
                {
                    output = (char)9474 + input[..(SizeX - 2)] + (char)9474;
                }
                break;
            case BorderStyle.Top:
                if (index == 0)
                {
                    output = new string((char)9472, SizeX);
                }
                break;
            case BorderStyle.Right:
                output = input[..(SizeX - 1)] + (char)9474;
                break;
            case BorderStyle.Bottom:
                if (index == SizeY - 1)
                {
                    output = new string((char)9472, SizeX);
                }
                break;
            case BorderStyle.Left:
                output = input[1..] + (char)9474;
                break;
        }
        return Task.FromResult(output);
    }
}
