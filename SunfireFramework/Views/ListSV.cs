using SunfireFramework.Views.TextBoxes;
using SunfireFramework.Enums;
using SunfireFramework.Terminal;
using SunfireFramework.Rendering;

namespace SunfireFramework.Views;

public class ListSV : IRelativeSunfireView
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

    public bool PopulatingSignal = false;

    private int startIndex = 0;
    public int selectedIndex = 0;

    public SVColor BackgroundColor { get; set; } = new() { R = 0, G = 0, B = 0 };

    private List<LabelSVSlim> VisibleLabels = [];
    private readonly List<LabelSVSlim> Labels = [];

    private string blankString = "";

    public Task AddLabel(LabelSVSlim label)
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

    public async Task UpdateVisibleLabels()
    {
        VisibleLabels = [.. Labels.Skip(startIndex).Take(SizeY)];
        for (int i = 0; i < VisibleLabels.Count; i++)
        {
            VisibleLabels[i].OriginX = OriginX;
            VisibleLabels[i].OriginY = OriginY + i;
            VisibleLabels[i].SizeX = SizeX;
            VisibleLabels[i].TextProperties &= ~SVTextProperty.Highlight;
        }
        Labels[selectedIndex].TextProperties |= SVTextProperty.Highlight;

        await Task.WhenAll(VisibleLabels.Select(v => v.Arrange()));
    }

    //Should be called when selectedIndex is updated
    public Task ArrangeLabels()
    {
        throw new NotImplementedException();
    }

    //Should be called when list needs to be redrawn
    public async Task Draw(SVContext context)
    {
        await Task.WhenAll(VisibleLabels.Select(v => v.Draw(new(v.OriginX, v.OriginY, context.Buffer))));
    }

}
