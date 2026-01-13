using Sunfire.Tui.Interfaces;
using Sunfire.Tui.Models;
using Sunfire.Views.Text;
using static Sunfire.Views.Text.LabelSVSlim;

namespace Sunfire.Views;

public class InfoView : ISunfireView
{
    public int OriginX { set; get; }
    public int OriginY { set; get; }
    public int SizeX { set; get; }
    public int SizeY { set; get; }

    public bool Dirty { set; get; }

    private readonly BorderSV _border;
    private readonly LabelSV _label;

    public static InfoView New(string info, string? title = null)
    {
        var segments = new LabelSegment[1]
        {
            new() { Text = info}
        };

        LabelSV label = new()
        {
            Segments = segments
        };

        return new(label, title);
    }

    public InfoView(LabelSV label, string? title = null)
    {
        _label = label;

        BorderSV border = new()
        {
            SubView = _label
        };

        if(!string.IsNullOrEmpty(title))
        {
            var segments = new LabelSegment[1]
            {
                new() { Text = title }
            };

            border.TitleLabel = new() 
            { 
                Segments = segments
            };
        }

        _border = border;
    }

    public void UpdateTitle(string newTitle)
    {
        var segments = new LabelSegment[1]
        {
            new() { Text = newTitle }
        };

        UpdateTitle(segments);
    }

    public void UpdateTitle(LabelSegment[] segments)
    {
        LabelSV label = new()
        {
            Segments = segments
        };

        UpdateTitle(label);
    }

    public void UpdateTitle(LabelSV label)
    {
        _border.TitleLabel = label;
    }

    public void UpdateInfo(string newInfo)
    {
        var segments = new LabelSegment[1]
        {
            new() { Text = newInfo }
        };

        UpdateInfo(segments);
    }

    public void UpdateInfo(LabelSegment[] segments)
    {
        _label.Segments = segments;
    }

    public async Task<bool> Arrange()
    {
        if(!Dirty)
            return false;

        (_border.OriginX, _border.OriginY, _border.SizeX, _border.SizeY) = (OriginX, OriginY, SizeX, SizeY);
        await _border.Arrange();

        return true;
    }

    public async Task Draw(SVContext context)
    {
        await _border.Draw(context);
    }

    public async Task Invalidate()
    {
        Dirty = true;

        await _border.Invalidate();
    }
}
