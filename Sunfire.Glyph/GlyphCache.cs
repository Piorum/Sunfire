using Wcwidth;
using Sunfire.Glyph.Models;

namespace Sunfire.Glyph;

public static class GlyphCache
{
    private static readonly List<GlyphData> glyphs = [];
    private static readonly Dictionary<string, int> index = [];

    private static void Add(string cluster, int id, GlyphData glyphData)
    {
        glyphs.Add(glyphData);
        index[cluster] = id;
    }

    public static void AddOrUpdate(string graphemeCluster, byte width)
    {
        if(index.TryGetValue(graphemeCluster, out var i))
        {
            glyphs[i] = glyphs[i] with { Width = width };
        }
        else
        {
            var realWidth = MeasureGramphemeCluster(graphemeCluster);
            GlyphData newGlyph = new() { GraphemeCluster = graphemeCluster, Width = width, RealWidth = realWidth };

            var newId = glyphs.Count;
            Add(graphemeCluster, newId, newGlyph);
        }
    }

    public static GlyphData Get(GlyphInfo info) =>
        glyphs[info.Id];

    internal static GlyphInfo GetOrAdd(string graphemeCluster)
    {
        if(index.TryGetValue(graphemeCluster, out var i))
            return new() { Id = i, Width = glyphs[i].Width };
        
        var width = MeasureGramphemeCluster(graphemeCluster);
        GlyphData newGlyph = new() { GraphemeCluster = graphemeCluster, Width = width, RealWidth = width };

        var newId = glyphs.Count;
        Add(graphemeCluster, newId, newGlyph);

        return new() { Id = newId, Width = width };
    }

    private static byte MeasureGramphemeCluster(string graphemeCluster) =>
        (byte)graphemeCluster.EnumerateRunes().Sum(r => UnicodeCalculator.GetWidth(r));
}
