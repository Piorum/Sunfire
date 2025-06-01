using SunfireFramework.TextBoxes;

namespace SunfireFramework.Views;

public class ListSV : IRelativeSunfireView
{
    public int X { get; set; }
    public int Y { get; set; }
    public int Z { get; set; }

    public int OriginX { set; get; }
    public int OriginY { set; get; }
    public int SizeX { set; get; }
    public int SizeY { set; get; }

    public bool PopulatingSignal = false;

    private int startIndex = 0;
    public int selectedIndex = 0;

    public ConsoleColor BackgroundColor { get; set; } = ConsoleColor.Black;

    private readonly List<SVLabel> VisibleLabels = [];
    public readonly List<SVLabel> Labels = [];

    //Should be called when Labels is updated
    public async Task Arrange()
    {
        //Don't bother rendering while items are being added
        if (PopulatingSignal)
        {
            //Load default loading Label, display
            return;
        }
        //Don't bother with other adjustments if there is no labels anyways
        if (Labels.Count == 0)
        {
            //Load default empty Label, disple
            return;
        }

        await Task.WhenAll(Setup(), PositionStartIndex(), UpdateVisibleLabels());
    }

    public Task Setup()
    {
        foreach (var label in Labels)
        {
            label.SizeX = SizeX;
            label.SizeY = 1;
            label.Highlighted = false; // Test code
        }
        Labels[selectedIndex].Highlighted = true; // Test code
        return Task.CompletedTask;
    }

    public Task PositionStartIndex()
    {
        var diff = selectedIndex - startIndex;
        var maxDiff = 0.6f * SizeY;
        var minDiff = 0.4f * SizeY;
        var maxStartIndex = Labels.Count - SizeY + 1;
        //startIndex above where it should me
        if (diff > maxDiff)
        {
            startIndex = selectedIndex - (int)maxDiff;
        }
        //startIndex below where it should be
        else if (diff < minDiff)
        {
            startIndex = selectedIndex - (int)minDiff;
        }
        startIndex = Math.Min(startIndex, maxStartIndex);

        return Task.CompletedTask;
    }

    public Task UpdateVisibleLabels()
    {
        VisibleLabels.Clear();
        VisibleLabels.AddRange(Labels.Skip(startIndex).Take(SizeY));
        for (int i = 0; i < VisibleLabels.Count; i++)
        {
            var index = i;
            VisibleLabels[index].OriginY = OriginY + index;
        }
        return Task.CompletedTask;
    }

    //Should be called when selectedIndex is updated
    public Task ArrangeLabels()
    {
        throw new NotImplementedException();
    }

    //Should be called when list background needs to be redrawn
    public async Task Draw()
    {
        var blankString = new string(' ', SizeX);

        Console.BackgroundColor = BackgroundColor;

        for (int i = 0; i < SizeY; i++)
        {
            Console.SetCursorPosition(OriginX, OriginY + i);
            Console.Write(blankString);
        }

        await DrawLabels();
    }

    //Should be called when list items need to be redrawn
    public async Task DrawLabels()
    {
        await Task.WhenAll(VisibleLabels.Select(v => v.Draw()));
    }

}
