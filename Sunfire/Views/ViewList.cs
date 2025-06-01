using System.Diagnostics;
using Sunfire.Enums;

namespace Sunfire.Views;

public class ViewList : View
{
    private int startIndex = 0;
    private int selectedIndex = 0;
    private readonly List<ViewLabel> visibleLabels = [];

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
                if(startIndex < SubViews.Count - SizeY + 1) startIndex++;
                if (SubViews[selectedIndex] is ViewLabel vl1)
                    vl1.Highlighted = false;
                selectedIndex++;
                if (SubViews[selectedIndex] is ViewLabel vl2)
                    vl2.Highlighted = true;
                await RePositionSubViews();
            }
        }
    }

    public override void Add(View subView)
    {
        subView.Y = SubViews.Count;
        base.Add(subView);
    }

    public async override Task Arrange(int WidthConstraint, int HeightConstraint)
    {
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
}
