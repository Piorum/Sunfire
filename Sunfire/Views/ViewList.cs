using Sunfire.Enums;

namespace Sunfire.Views;

public class ViewList : View
{
    private static readonly ViewLabel Loading = new()
    {
        X = 0,
        Y = 0,
        FillStyleHeight = FillStyle.Min,
        FillStyleWidth = FillStyle.Max,
        SizeY = 1
    };

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
    }

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
                if(startIndex > 0) startIndex--;
                if (SubViews[selectedIndex] is ViewLabel vl1)
                    vl1.Highlighted = false;
                selectedIndex--;
                if (SubViews[selectedIndex] is ViewLabel vl2)
                    vl2.Highlighted = true;
                await RePositionSubViews();
            }
        }
    }

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
            SubViews.Remove(SubViews[selectedIndex]);

            for (int i = 0; i < SubViews.Count; i++)
            {
                SubViews[i].Y = i;
            }
        }
        else
        {
            List<View> selectedViews = [];
            foreach (var index in selected)
            {
                selectedViews.Add(SubViews[index]);
            }
            selected.Clear();
            foreach (var view in selectedViews)
            {
                SubViews.Remove(view);
            }

            selectedIndex -= selectedViews.Count;

            for (int i = 0; i < SubViews.Count; i++)
            {
                SubViews[i].Y = i;
            }
        }

        await PopulateXYLevels();

        if (selectedIndex < 0) selectedIndex = 0;
        else if (!yLevels!.Contains(selectedIndex)) selectedIndex = SubViews.Count - 1;

        if (selectedIndex < startIndex) startIndex = selectedIndex - (int)(0.4f * SizeY);
        else if (selectedIndex > startIndex + SizeY) startIndex = selectedIndex - (int)(0.6f * SizeY);

        await RePositionSubViews();
    }

    public async override Task Arrange(int WidthConstraint, int HeightConstraint)
    {
        if (SubViews.Count == 0)
        {
            await DrawLoading(WidthConstraint, HeightConstraint);
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
    }

    private async Task DrawLoading(int WidthConstraint, int HeightConstraint)
    {
        Loading.OriginX = BorderStyle switch
        {
            BorderStyle.Full or BorderStyle.Left => OriginX + 1,
            _ => OriginX
        };

        Loading.OriginY = BorderStyle switch
        {
            BorderStyle.Full or BorderStyle.Top => OriginY + 1,
            _ => OriginY
        };

        Loading.SizeX = BorderStyle switch
        {
            BorderStyle.Full => WidthConstraint - 2,
            BorderStyle.Left or BorderStyle.Right => WidthConstraint - 1,
            _ => WidthConstraint
        };

        await Draw();
        await Loading.Arrange(WidthConstraint, HeightConstraint);
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
