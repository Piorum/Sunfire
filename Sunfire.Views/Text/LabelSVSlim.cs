using Sunfire.Tui.Models;
using Sunfire.Tui.Enums;
using Sunfire.Tui.Interfaces;
using Sunfire.Ansi.Models;
using Sunfire.Views.Enums;
using System.Text;
using Sunfire.Glyph.Models;
using Sunfire.Glyph;

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

    private readonly List<GlyphInfo> glyphs = [];
    private readonly List<byte> styles = [];
    private readonly List<StyleData> styleMap = [];
    private readonly Dictionary<StyleData, byte> styleIndex = [];

    public readonly struct LabelSegment()
    {
        readonly public string Text { get; init; } = string.Empty;
        readonly public StyleData Style { get; init; } = new();
    }

    public async Task<bool> Arrange()
    {
        await OnArrange();

        if (Dirty)
        {
            if(Segments is not null && Segments.Length > 0)
            {
                glyphs.Clear();
                styles.Clear();
                styleMap.Clear();
                styleIndex.Clear();

                foreach(var segement in Segments)
                {
                    if(string.IsNullOrEmpty(segement.Text))
                        continue;

                    if (!styleIndex.TryGetValue(segement.Style, out var style_id))
                    {
                        style_id = (byte)styleMap.Count;
                        styleMap.Add(segement.Style);
                        styleIndex[segement.Style] = style_id;
                    }

                    foreach (var glyph in GlyphFactory.GetGlyphs(segement.Text))
                    {
                        glyphs.Add(glyph);
                        styles.Add(style_id);
                    }
                }
            }
            else
            {
                glyphs.Clear();
                styles.Clear();
                styleMap.Clear();
                styleIndex.Clear();
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

        var textLen = glyphs.Sum(g => g.Width);

        int startX = Alignment == Direction.Right
            ? SizeX - textLen
            : 0;

        int minX = Math.Max(0, startX);
        int maxX = Math.Min(SizeX, startX + textLen);

        bool isSelected = (LabelProperties & LabelSVProperty.Selected) != 0;

        SVCell paddingCell;
        StyleData selectedStyle = default;

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

            int glyphIndex = minX - startX;
            for (int x = minX; x < maxX; x++)
            {
                var style = styleMap[styles[glyphIndex]];

                var renderStyle = isSelected && style.BackgroundColor is null
                    ? selectedStyle
                    : style;

                var glyph = glyphs[glyphIndex];

                var newCell = new SVCell(
                    glyph, 
                    renderStyle.ForegroundColor, 
                    renderStyle.BackgroundColor, 
                    renderStyle.Properties);

                context[x, y] = newCell;

                if(glyph.Width == 2 && x+1 < maxX)
                {
                    context[x+1, y] = new();
                    x++;
                }
                glyphIndex++;
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
