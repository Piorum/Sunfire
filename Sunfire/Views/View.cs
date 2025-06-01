using Sunfire.Enums;

namespace Sunfire.Views;

public class View
{
    public string Tag = "";

    required public int X;
    required public int Y;

    public int OriginX { internal set; get; } // Top Left
    public int OriginY { internal set; get; } // Top Left
    public int SizeX { internal set; get; } // Width
    public int SizeY { internal set; get; } // Height

    required public FillStyle FillStyleWidth;
    required public FillStyle FillStyleHeight;
    public float WidthPercent = 1.0f; //1.0f == 100%
    public float HeightPercent = 1.0f; //1.0f == 100%

    public BorderStyle BorderStyle = BorderStyle.None;
    public ConsoleColor BackgroundColor = ConsoleColor.Black;

    public List<View> SubViews { get; } = [];
    public View? Container { get; internal set; } = null;

    public List<int>? xLevels;
    public List<int>? yLevels;

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

    protected virtual async Task Measure(int WidthConstraint, int HeightConstraint)
    {
        SizeX = WidthConstraint;
        SizeY = HeightConstraint;

        if(xLevels is null || yLevels is null)
            await PopulateXYLevels();

        int baseWidth = SizeX - BorderStyle switch
        {
            BorderStyle.Full => 2,
            BorderStyle.Left or BorderStyle.Right => 1,
            _ => 0
        };
        int baseHeight = SizeY - BorderStyle switch
        {
            BorderStyle.Full => 2,
            BorderStyle.Top or BorderStyle.Bottom => 1,
            _ => 0
        };

        Dictionary<int, int> availableWidth = yLevels!.ToDictionary(y => y, _ => baseWidth);
        Dictionary<int, int> availableHeight = xLevels!.ToDictionary(x => x, _ => baseHeight);

        //Width
        var orderByWidthStyle = SubViews.OrderBy(sv => (int)sv.FillStyleWidth).ToList();

        foreach (var view in orderByWidthStyle)
        {
            switch (view.FillStyleWidth)
            {
                case FillStyle.Min:
                    view.SizeX = 1;
                    availableWidth[view.Y]--;
                    break;
                case FillStyle.Percent:
                    view.SizeX = (int)(availableWidth[view.Y] * view.WidthPercent);
                    availableWidth[view.Y] -= view.SizeX;
                    break;
                case FillStyle.Max:
                    view.SizeX = availableWidth[view.Y];
                    break;
            }
        }

        //Height
        var orderByHeightStyle = SubViews.OrderBy(sv => sv.FillStyleHeight).ToList();

        foreach (var view in orderByHeightStyle)
        {
            switch (view.FillStyleHeight)
            {
                case FillStyle.Min:
                    view.SizeY = 1;
                    foreach (var xLevel in xLevels!)
                    {
                        availableHeight[xLevel]--;
                    }
                    break;
                case FillStyle.Percent:
                    view.SizeY = (int)(availableHeight[view.X] * view.HeightPercent);
                    foreach (var xLevel in xLevels!)
                    {
                        availableHeight[xLevel] -= view.SizeY;
                    }
                    break;
                case FillStyle.Max:
                    view.SizeY = availableHeight[view.X];
                    break;
            }
        }
    }

    protected virtual async Task Position()
    {
        if(xLevels is null || yLevels is null)
            await PopulateXYLevels();

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
        var subViewGrid = SubViews.ToLookup(sv => (sv.X, sv.Y));
        foreach (var yLevel in yLevels!)
        {
            int largestY = 0;
            foreach (var xLevel in xLevels!)
            {
                View? view = subViewGrid[(xLevel, yLevel)].FirstOrDefault();
                if (view is null) continue;

                view.OriginX = CursorPosX;
                view.OriginY = CursorPosY;

                CursorPosX += view.SizeX;

                if (view.SizeY > largestY) largestY = view.SizeY;
            }
            CursorPosX = StartCursorPosX;
            CursorPosY += largestY;
        }
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

            Console.Write(output);
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

    protected Task PopulateXYLevels()
    {
        HashSet<int> xSet = [];
        HashSet<int> ySet = [];

        foreach (var view in SubViews)
        {
            xSet.Add(view.X);
            ySet.Add(view.Y);
        }

        xLevels = [.. xSet.Order()];
        yLevels = [.. ySet.Order()];
        
        return Task.CompletedTask;
    }
}
