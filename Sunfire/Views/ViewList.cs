using Sunfire.Enums;

namespace Sunfire.Views;

//This whole class is very complex and nearly entirely overrides View
//Holds all of the logic needed to navigate as well as add, remove, and select items - Should this be moved up to AppState?
public class ViewList : View
{
    //store default labels somewhere else?
    private static readonly ViewLabel Loading = new()
    {
        X = 0,
        Y = 0,
        FillStyleHeight = FillStyle.Min,
        FillStyleWidth = FillStyle.Max,
        SizeY = 1
    };
    private static readonly ViewLabel Empty = new()
    {
        X = 0,
        Y = 0,
        FillStyleHeight = FillStyle.Min,
        FillStyleWidth = FillStyle.Max,
        SizeY = 1,
        TextColor = ConsoleColor.DarkGray
    };

    //works but feels kinda forced
    public bool LoadingSignal;

    private int startIndex = 0;
    private int selectedIndex = 0;
    private readonly List<ViewLabel> visibleLabels = [];

    private readonly List<int> selected = [];

    static ViewList()
    {
        Loading.TextFields.Add(new()
        {
            Text = "..."
        });
        Empty.TextFields.Add(new()
        {
            Text = "Empty"
        });
    }

    //potentially optimizable
    //add function to move more than one at a time
    public async Task MoveUp()
    {
        if ((selectedIndex - 1) >= 0)
        {
            if ((selectedIndex - startIndex) > (0.4f * SizeY))
            {
                if (SubViews[selectedIndex] is ViewLabel vl1)
                    vl1.Highlighted = false;
                await SubViews[selectedIndex].Draw();
                selectedIndex--;
                if (SubViews[selectedIndex] is ViewLabel vl2)
                    vl2.Highlighted = true;
                await SubViews[selectedIndex].Draw();
            }
            else
            {
                if (startIndex > 0) startIndex--;
                if (SubViews[selectedIndex] is ViewLabel vl1)
                    vl1.Highlighted = false;
                selectedIndex--;
                if (SubViews[selectedIndex] is ViewLabel vl2)
                    vl2.Highlighted = true;
                await RePositionSubViews();
            }
        }
    }

    //should probably be merged with move up
    public async Task MoveDown()
    {
        if ((selectedIndex + 1) < SubViews.Count)
        {
            if ((selectedIndex - startIndex) < (0.6f * SizeY))
            {
                if (SubViews[selectedIndex] is ViewLabel vl1)
                    vl1.Highlighted = false;
                await SubViews[selectedIndex].Draw();
                selectedIndex++;
                if (SubViews[selectedIndex] is ViewLabel vl2)
                    vl2.Highlighted = true;
                await SubViews[selectedIndex].Draw();
            }
            else
            {
                if (startIndex < SubViews.Count - SizeY + 1) startIndex++;
                selectedIndex++;
                await RePositionSubViews();
            }
        }
        else if (selectedIndex == SubViews.Count - 1)
        {
            await SubViews[selectedIndex].Draw();
        }
    }

    public async Task ToTop()
    {
        selectedIndex = 0;
        startIndex = 0;
        await RePositionSubViews();
    }

    public async Task ToBottom()
    {
        selectedIndex = SubViews.Count - 1;
        startIndex = Math.Max(0, selectedIndex - SizeY + 2);
        await RePositionSubViews();
    }

    public async Task SelectCurrent()
    {
        if (SubViews[selectedIndex] is ViewLabel vl)
        {
            if (vl.Selected)
            {
                vl.Selected = false;
                selected.Remove(selectedIndex);
            }
            else
            {
                vl.Selected = true;
                selected.Add(selectedIndex);
            }
        }
        await MoveDown();
    }

    public override void Add(View subView)
    {
        if (subView is not ViewLabel)
            throw new Exception("View other than ViewLabel was added to a ViewList");

        subView.Y = SubViews.Count;
        base.Add(subView);
    }

    public void Add(List<ViewLabel> subViews)
    {
        int iterations = 0;
        foreach (var view in subViews)
        {
            view.Y = SubViews.Count + iterations;
            view.Container = this;
            iterations++;
        }
        SubViews.AddRange(subViews);
    }

    public async Task Remove()
    {
        if (selected.Count == 0)
        {
            SubViews.RemoveAt(selectedIndex);
        }
        else
        {
            var sortedIndices = selected.OrderDescending().ToList();
            foreach (var index in sortedIndices)
            {
                SubViews.RemoveAt(index);
            }
            selected.Clear();

            selectedIndex = Math.Max(0, selectedIndex - sortedIndices.Count);
        }

        for (int i = 0; i < SubViews.Count; i++)
            SubViews[i].Y = i;

        await PopulateXYLevels();

        if (selectedIndex < 0)
            selectedIndex = 0;
        else if (!yLevels!.Contains(selectedIndex))
            selectedIndex = Math.Max(0, SubViews.Count - 1);

        if (selectedIndex < startIndex)
            startIndex = selectedIndex - (int)(0.4f * SizeY);
        else if (selectedIndex > startIndex + SizeY)
            startIndex = selectedIndex - (int)(0.6f * SizeY);

        if (SubViews.Count > 0)
            await RePositionSubViews();
        else
            await Arrange(SizeX, SizeY);
    }

    public async override Task Arrange(int WidthConstraint, int HeightConstraint)
    {
        if (LoadingSignal)
        {
            await DisplaySingle(Loading, WidthConstraint, HeightConstraint);
            return;
        }

        await Measure(WidthConstraint, HeightConstraint);

        var diff = selectedIndex - startIndex;
        var maxDiff = 0.6f * SizeY;
        var minDiff = 0.4f * SizeY;
        var maxStartIndex = SubViews.Count - SizeY + 1;
        if (diff > maxDiff)
        {
            startIndex = selectedIndex - (int)maxDiff;
        }
        else if (diff < minDiff)
        {
            startIndex = selectedIndex - (int)minDiff;
        }
        startIndex = Math.Min(startIndex, maxStartIndex);

        visibleLabels.Clear();

        await Position();

        await Draw();

        List<Task> arrangeTasks = [];
        foreach (var view in visibleLabels)
        {
            if (view.Y == selectedIndex) view.Highlighted = true;
            else view.Highlighted = false;

            arrangeTasks.Add(view.Arrange(view.SizeX, view.SizeY));
        }
        await Task.WhenAll(arrangeTasks);

        if (visibleLabels.Count == 0 && !LoadingSignal)
        {
            await DisplaySingle(Empty, WidthConstraint, HeightConstraint);
        }
    }

    private async Task DisplaySingle(View view, int WidthConstraint, int HeightConstraint)
    {
        view.OriginX = BorderStyle switch
        {
            BorderStyle.Full or BorderStyle.Left => OriginX + 1,
            _ => OriginX
        };

        view.OriginY = BorderStyle switch
        {
            BorderStyle.Full or BorderStyle.Top => OriginY + 1,
            _ => OriginY
        };

        view.SizeX = BorderStyle switch
        {
            BorderStyle.Full => WidthConstraint - 2,
            BorderStyle.Left or BorderStyle.Right => WidthConstraint - 1,
            _ => WidthConstraint
        };

        await Draw();
        await view.Arrange(WidthConstraint, HeightConstraint);
    }

    protected override async Task Measure(int WidthConstraint, int HeightConstraint)
    {
        SizeX = WidthConstraint;
        SizeY = HeightConstraint;

        if (xLevels is null || yLevels is null)
            await PopulateXYLevels();

        int baseWidth = SizeX - BorderStyle switch
        {
            BorderStyle.Full => 2,
            BorderStyle.Left or BorderStyle.Right => 1,
            _ => 0
        };

        //Width
        foreach (var view in SubViews)
        {
            view.SizeX = baseWidth;
        }

        //Height
        foreach (var view in SubViews)
        {
            view.SizeY = 1;
        }
    }

    private async Task RePositionSubViews()
    {
        var filledSpots = visibleLabels.Select(l => l.OriginY).ToHashSet();

        visibleLabels.Clear();
        await Position();

        var newSpots = visibleLabels.Select(l => l.OriginY).ToHashSet();

        var duplicateSpots = filledSpots.Except(newSpots);

        var availableWidth = BorderStyle switch
        {
            BorderStyle.Full => SizeX - 2,
            BorderStyle.Left or BorderStyle.Right => SizeX - 1,
            _ => SizeX
        };

        var leftPos = OriginX;
        if (BorderStyle is BorderStyle.Full or BorderStyle.Left)
            leftPos++;

        var blankLine = new string(' ', availableWidth);
        Console.ForegroundColor = ConsoleColor.Black;
        Console.BackgroundColor = ConsoleColor.Black;

        foreach (var spot in duplicateSpots)
        {
            Console.SetCursorPosition(leftPos, spot);
            Console.Write(new string(' ', availableWidth));
        }

        List<Task> arrangeTasks = [];
        foreach (var view in visibleLabels)
        {
            if (view.Y == selectedIndex) view.Highlighted = true;
            else view.Highlighted = false;

            arrangeTasks.Add(view.Arrange(view.SizeX, view.SizeY));
        }
        await Task.WhenAll(arrangeTasks);
    }

    protected override Task Position()
    {
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

        var refinedLabels = SubViews.OfType<ViewLabel>().Skip(startIndex).Take(SizeY);
        foreach (var label in refinedLabels)
        {
            label.OriginX = CursorPosX;
            label.OriginY = CursorPosY;

            visibleLabels.Add(label);

            CursorPosX = StartCursorPosX;
            CursorPosY += 1;
        }

        return Task.CompletedTask;
    }

    protected override Task PopulateXYLevels()
    {
        xLevels = [1];

        yLevels = [.. Enumerable.Range(0, SubViews.Count)];

        return Task.CompletedTask;
    }
}
