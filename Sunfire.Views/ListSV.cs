using Sunfire.Tui.Enums;
using Sunfire.Tui.Models;
using Sunfire.Tui.Interfaces;
using Sunfire.Ansi.Models;
using Sunfire.Views.Text;

namespace Sunfire.Views;

public class ListSV : IRelativeSunfireView
{
    public int X { get; set; }
    public int Y { get; set; }
    public int Z { get; set; }

    public FillStyle FillStyleX  { set; get; } = FillStyle.Max;
    public FillStyle FillStyleY  { set; get; } = FillStyle.Max;
    public int StaticX  { set; get; } = 1; //1 = 1 Cell
    public int StaticY  { set; get; } = 1; //1 = 1 Cell
    public float PercentX  { set; get; } = 1.0f; //1.0f == 100%
    public float PercentY  { set; get; } = 1.0f; //1.0f == 100%

    public int MinX { get; } = 0;
    public int MinY { get; } = 0;

    public int OriginX { set; get; }
    public int OriginY { set; get; }
    public int SizeX { set; get; }
    public int SizeY { set; get; }

    public bool Dirty { set; get; }

    private int startIndex = 0;
    public int SelectedIndex = 0;
    public int MaxIndex => Labels.Count - 1;

    public SColor? BackgroundColor { get; set; } = null;

    private List<LabelSVSlim> VisibleLabels = [];
    private readonly List<LabelSVSlim> Labels = [];

    public Task AddLabel(LabelSVSlim label)
    {
        label.SizeY = 1;
        Labels.Add(label);
        return Task.CompletedTask;
    }

    //Should be called when Labels is updated
    public async Task<bool> Arrange()
    {
        if (Dirty)
        {
            await OnArrange();
            Dirty = false;

            return true; //Work Done
        }

        return false; //No Work Done
    }

    private async Task OnArrange()
    {
        //Don't bother with other adjustments if there is no labels anyways
        if (Labels.Count == 0)
        {
            //Load default empty Label, disple
            return;
        }

        await PositionStartIndex();
        await UpdateVisibleLabels();
    }

    public async Task PositionStartIndex()
    {
        var diff = SelectedIndex - startIndex;
        var maxDiff = (int)(0.6f * SizeY);
        var minDiff = (int)(0.4f * SizeY);
        var maxStartIndex = Labels.Count - SizeY + 1;

        //Ensure proper resizing
        if (Labels.Count - startIndex < SizeY - 1 && Labels.Count >= SizeY - 1)
        {
            startIndex = 0;
            await PositionStartIndex();
            return;
        }

        //startIndex above where it should me
        if (diff > maxDiff)
        {
            startIndex = Math.Min(SelectedIndex - maxDiff, maxStartIndex);
        }
        //startIndex below where it should be
        else if (diff < minDiff)
        {
            startIndex = Math.Max(SelectedIndex - minDiff, 0);
        }
    }

    public async Task UpdateVisibleLabels()
    {
        VisibleLabels = [.. Labels.Skip(startIndex).Take(SizeY)];
        for (int i = 0; i < VisibleLabels.Count; i++)
        {
            VisibleLabels[i].OriginX = OriginX;
            VisibleLabels[i].OriginY = OriginY + i;
            VisibleLabels[i].SizeX = SizeX;
            VisibleLabels[i].TextProperties &= ~SAnsiProperty.Highlight;
        }
        Labels[SelectedIndex].TextProperties |= SAnsiProperty.Highlight;

        await Task.WhenAll(VisibleLabels.Select(v => v.Invalidate()));
        await Task.WhenAll(VisibleLabels.Select(v => v.Arrange()));
    }

    //Should be called when list needs to be redrawn
    public async Task Draw(SVContext context)
    {
        //Should actually draw background here

        await Task.WhenAll(VisibleLabels.Select(v => v.Draw(new(v.OriginX, v.OriginY, context.Buffer))));
    }

    public Task Invalidate()
    {
        Dirty = true;
        return Task.CompletedTask;
    }
}
