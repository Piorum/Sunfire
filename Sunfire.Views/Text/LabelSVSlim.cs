using Sunfire.Tui.Models;
using Sunfire.Tui.Enums;
using Sunfire.Tui.Interfaces;
using Sunfire.Ansi.Models;
using Sunfire.Views.Enums;
using System.Text;

namespace Sunfire.Views.Text;

public class LabelSVSlim : ISunfireView
{
    public int OriginX { set; get; }
    public int OriginY { set; get; }
    public int SizeX { set; get; }
    public int SizeY { set; get; }

    public bool Dirty { set; get; }

    public LabelSVProperty LabelProperties = LabelSVProperty.None;
    public Direction Alignment = Direction.Left;

    public LabelSegment[]? _segments;
    public LabelSegment[]? Segments
    {
        get => _segments;
        set
        {
            _segments = value;
            Dirty = true;
        }
    }

    protected SColor? tagColor = null;

    public string rawText = "";
    public SStyle[] styleMap = [];

    public readonly struct LabelSegment()
    {
        readonly public string Text { get; init; } = string.Empty;
        readonly public SStyle Style { get; init; } = new();
    }

    public async Task<bool> Arrange()
    {
        await OnArrange();

        if (Dirty)
        {
            if(Segments is not null && Segments.Length > 0)
            {
                int totalLength = Segments.Sum(s => s.Text.Length);

                styleMap = new SStyle[totalLength];

                var sb = new StringBuilder(totalLength);

                int currentIndex = 0;
                foreach(var segment in Segments)
                {
                    if (string.IsNullOrEmpty(segment.Text)) 
                        continue;
                    
                    sb.Append(segment.Text);
                    Array.Fill(styleMap, segment.Style, currentIndex, segment.Text.Length);

                    currentIndex += segment.Text.Length;
                }

                rawText = sb.ToString();
            }
            else
            {
                rawText = string.Empty;
                styleMap = [];
            }

            Dirty = false;
            return true;
        }
        return false;
    }   

    protected virtual Task OnArrange() => Task.CompletedTask;

    public Task Draw(SVContext context)
    {
        if(Segments is null || Segments.Length == 0)
            return Task.CompletedTask;

        var textLen = rawText.Length;

        int startX = Alignment == Direction.Right
            ? SizeX - textLen
            : 0;

        int minX = Math.Max(0, startX);
        int maxX = Math.Min(SizeX, startX + textLen);

        bool isSelected = (LabelProperties & LabelSVProperty.Selected) != 0;

        SVCell paddingCell;
        SStyle selectedStyle = default;

        if(isSelected)
        {
            var lastSegmentStyle = Alignment == Direction.Left 
                ? Segments[^1].Style
                : Segments[1].Style;

            selectedStyle = lastSegmentStyle with { Properties = lastSegmentStyle.Properties | SAnsiProperty.Highlight };

            paddingCell = SVCell.Blank with
            {
                ForegroundColor = selectedStyle.ForegroundColor,
                BackgroundColor = selectedStyle.BackgroundColor,
                Properties = selectedStyle.Properties
            };
        }
        else
            paddingCell = SVCell.Blank;

        for (int y = 0; y < SizeY; y++)
        {
            for(int x = 0; x < minX; x++)
                context[x, y] = paddingCell;

            for (int x = minX; x < maxX; x++)
            {
                int charIndex = x - startX;
                var style = styleMap[charIndex];

                var renderStyle = isSelected && style.BackgroundColor is null
                    ? selectedStyle
                    : style;

                var newCell = new SVCell(
                    new(rawText[charIndex]), 
                    renderStyle.ForegroundColor, 
                    renderStyle.BackgroundColor, 
                    renderStyle.Properties);

                context[x, y] = newCell;
            }

            for(int x = maxX; x < SizeX; x++)
                context[x, y] = paddingCell;
        }

        if(tagColor is not null)
            if(Alignment == Direction.Left)
                context[SizeX - 1, SizeY - 1] = SVCell.Blank with { BackgroundColor = tagColor };
            else
                context[0, 0] = SVCell.Blank with { BackgroundColor = tagColor };

        return Task.CompletedTask;
    }

    public Task Invalidate()
    {
        return Task.CompletedTask;
    }
}
