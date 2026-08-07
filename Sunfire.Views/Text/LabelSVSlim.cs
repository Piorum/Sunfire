using Moonfire.Rendering.Models;
using Moonfire.Rendering.Enums;
using Moonfire.Rendering.Interfaces;
using Moonfire.Ansi.Models;
using Sunfire.Views.Enums;
using Moonfire.Glyph;
using Moonfire.Ansi;

namespace Sunfire.Views.Text;

public class LabelSVSlim : IMoonfireView
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

    protected AnsiTruecolor? tagColor = null;

    private readonly List<(int id, byte width)> glyphs = [];
    private readonly List<byte> styles = [];
    private readonly List<AnsiStyleData> styleMap = [];
    private readonly Dictionary<AnsiStyleData, byte> styleIndex = [];

    public readonly struct LabelSegment()
    {
        readonly public string Text { get; init; } = string.Empty;
        readonly public AnsiStyleData Style { get; init; } = new();
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

    public Task Draw(TerminalContext context)
    {
        if(Segments is null || Segments.Length == 0)
            return Task.CompletedTask;

        var textLen = glyphs.Sum(g => g.width);

        int startX = Alignment == Direction.Right
            ? (int)context.W - textLen
            : 0;

        int minX = Math.Max(0, startX);
        int maxX = Math.Min((int)context.W, startX + textLen);

        bool isSelected = (LabelProperties & LabelSVProperty.Selected) != 0;

        TerminalCell paddingCell;
        AnsiStyleData selectedStyle = new();

        if(isSelected)
        {
            var lastSegmentStyle = Alignment == Direction.Left 
                ? Segments[^1].Style
                : Segments[0].Style;

            selectedStyle = lastSegmentStyle with { Properties = lastSegmentStyle.Properties | AnsiProperty.Highlight };

            var paddingStyleId = AnsiStyleFactory.GetStyleId((selectedStyle.ForegroundColor, selectedStyle.BackgroundColor, selectedStyle.Properties));
            paddingCell = new(TerminalCell.Blank.GlyphId, TerminalCell.Blank.Width, paddingStyleId);
        }
        else
            paddingCell = TerminalCell.Blank;

        for (int y = 0; y < context.H; y++)
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
                var renderStyleId = AnsiStyleFactory.GetStyleId((renderStyle.ForegroundColor, renderStyle.BackgroundColor, renderStyle.Properties));

                var (id, width) = glyphs[glyphIndex];

                var newCell = new TerminalCell(
                    id,
                    width, 
                    renderStyleId);

                context[x, y] = newCell;

                if(width == 2 && x+1 < maxX)
                {
                    x++;
                    context[x, y] = new(0,1,renderStyleId);
                }
                glyphIndex++;
            }

            for(int x = maxX; x < context.W; x++)
                context[x, y] = paddingCell;
        }

        if(tagColor is not null)
        {
            var tagStyleId = AnsiStyleFactory.GetStyleId((null, tagColor, AnsiProperty.None));
            TerminalCell tagCell = new(TerminalCell.Blank.GlyphId, TerminalCell.Blank.Width, tagStyleId);
        
            if(Alignment == Direction.Left)
                context[(int)context.W - 1, (int)context.H - 1] = tagCell;
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
