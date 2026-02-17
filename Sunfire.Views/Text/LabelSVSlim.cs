using Sunfire.Tui.Models;
using Sunfire.Tui.Enums;
using Sunfire.Tui.Interfaces;
using Sunfire.Ansi.Models;
using Sunfire.Views.Enums;
using System.Text;
using Sunfire.Glyph.Models;
using Sunfire.Glyph;
using Sunfire.Ansi;

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

    private readonly List<(int id, byte width)> glyphs = [];
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

                    foreach (var glyph in GlyphFactory.GetGlyphIds(segement.Text))
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

        var textLen = glyphs.Sum(g => g.width);

        int startX = Alignment == Direction.Right
            ? SizeX - textLen
            : 0;

        int minX = Math.Max(0, startX);
        int maxX = Math.Min(SizeX, startX + textLen);

        bool isSelected = (LabelProperties & LabelSVProperty.Selected) != 0;

        SVCell paddingCell;
        StyleData selectedStyle = new();

        if(isSelected)
        {
            var lastSegmentStyle = Alignment == Direction.Left 
                ? Segments[^1].Style
                : Segments[1].Style;

            selectedStyle = lastSegmentStyle with { Properties = lastSegmentStyle.Properties | SAnsiProperty.Highlight };

            var paddingStyleId = StyleFactory.GetStyleId((selectedStyle.ForegroundColor, selectedStyle.BackgroundColor, selectedStyle.Properties));
            paddingCell = new(SVCell.Blank.GlyphId, SVCell.Blank.Width, paddingStyleId);
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
                var renderStyleId = StyleFactory.GetStyleId((renderStyle.ForegroundColor, renderStyle.BackgroundColor, renderStyle.Properties));

                var (id, width) = glyphs[glyphIndex];

                var newCell = new SVCell(
                    id,
                    width, 
                    renderStyleId);

                context[x, y] = newCell;

                if(width == 2 && x+1 < maxX)
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
        {
            var tagStyleId = StyleFactory.GetStyleId((null, tagColor, SAnsiProperty.None));
            SVCell tagCell = new(SVCell.Blank.GlyphId, SVCell.Blank.Width, tagStyleId);
        
            if(Alignment == Direction.Left)
                context[SizeX - 1, SizeY - 1] = tagCell;
            else
                context[0, 0] = tagCell;
        }

        return Task.CompletedTask;
    }

    public Task Invalidate()
    {
        return Task.CompletedTask;
    }
}
