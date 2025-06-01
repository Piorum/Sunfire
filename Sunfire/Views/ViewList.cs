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
        var filledSpots = visibleLabels.Select(l => l.OriginY).ToList();
        visibleLabels.Clear();
        await Position();
        var newSpots = visibleLabels.Select(l => l.OriginY).ToList();
        var duplicateSpots = filledSpots.Where(s => !newSpots.Contains(s));

        var availableWidth = SizeX;
        var leftPos = OriginX;
        switch (BorderStyle)
        {
            case BorderStyle.Full:
                availableWidth -= 2;
                OriginX++;
                break;
            case BorderStyle.Left:
                availableWidth--;
                OriginX++;
                break;
            case BorderStyle.Right:
                availableWidth--;
                break;
        }

        foreach (var spot in duplicateSpots)
        {
            Console.SetCursorPosition(leftPos, spot);
            Console.ForegroundColor = ConsoleColor.Black;
            Console.BackgroundColor = ConsoleColor.Black;
            await Console.Out.WriteAsync(new string(' ', availableWidth));
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

        foreach (var yLevel in yLevels)
        {
            if (yLevel < startIndex) continue;

            int largestY = 0;
            foreach (var xLevel in xLevels)
            {
                ViewLabel? view = SubViews.OfType<ViewLabel>().Where(v => v.X == xLevel && v.Y == yLevel).FirstOrDefault();
                if (view is null) continue;

                view.OriginX = CursorPosX;
                view.OriginY = CursorPosY;

                CursorPosX += view.SizeX;

                if (view.SizeY > largestY) largestY = view.SizeY;
                if (view.OriginY < (StartCursorPosY + SizeY)) visibleLabels.Add(view);
            }
            CursorPosX = StartCursorPosX;
            CursorPosY += largestY;
        }

        return Task.CompletedTask;
    }
}
