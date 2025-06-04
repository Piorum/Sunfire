using SunfireFramework.TextBoxes;
using SunfireFramework.Enums;

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

    private List<SVLabel> VisibleLabels = [];
    private readonly List<SVLabel> Labels = [];

    private string blankString = "";

    public Task AddLabel(SVLabel label)
    {
        label.SizeY = 1;
        Labels.Add(label);
        return Task.CompletedTask;
    }

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

        blankString = new string(' ', SizeX);

        await PositionStartIndex();
        await UpdateVisibleLabels();

        //await Task.WhenAll(Setup(), PositionStartIndex(), UpdateVisibleLabels());
    }

    public Task PositionStartIndex()
    {
        var diff = selectedIndex - startIndex;
        var maxDiff = (int)(0.6f * SizeY);
        var minDiff = (int)(0.4f * SizeY);
        var maxStartIndex = Labels.Count - SizeY + 1;

        //startIndex above where it should me
        if (diff > maxDiff)
        {
            startIndex = Math.Min(selectedIndex - maxDiff, maxStartIndex);
        }
        //startIndex below where it should be
        else if (diff < minDiff)
        {
            startIndex = Math.Max(selectedIndex - minDiff, 0);
        }

        return Task.CompletedTask;
    }

    public Task UpdateVisibleLabels()
    {
        VisibleLabels = [.. Labels.Skip(startIndex).Take(SizeY)];
        for (int i = 0; i < VisibleLabels.Count; i++)
        {
            VisibleLabels[i].OriginX = OriginX;
            VisibleLabels[i].OriginY = OriginY + i;
            VisibleLabels[i].SizeX = SizeX;
            VisibleLabels[i].Properties.Remove(TextProperty.Highlighted);
        }
        Labels[selectedIndex].Properties.Add(TextProperty.Highlighted);
        return Task.CompletedTask;
    }

    //Should be called when selectedIndex is updated
    public Task ArrangeLabels()
    {
        throw new NotImplementedException();
    }

    //Should be called when list needs to be redrawn
    public async Task Draw()
    {
        for (int i = 0; i < VisibleLabels.Count; i++)
        {
            await VisibleLabels[i].Draw();
        }

        List<ConsoleOutput> outputs = [];
        for (int i = VisibleLabels.Count; i < SizeY; i++)
        {
            outputs.Add(new()
            {
                X = OriginX,
                Y = OriginY + i,
                Output = blankString
            });
        }
        await ConsoleWriter.WriteAsync(outputs, backgroundColor: BackgroundColor);
    }

}
