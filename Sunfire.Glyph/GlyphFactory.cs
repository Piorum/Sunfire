using System.Globalization;

namespace Sunfire.Glyph;

public static class GlyphFactory
{
    private readonly static GlyphCache glyphCache = new();

    private readonly static (int id, byte width) invalidGlyph = GetGlyphs("\uFFFD").First();

    public static List<(int id, byte width)> GetGlyphs(string text)
    {
        List<(int id, byte width)> glyphs = [];

        var enumerator = StringInfo.GetTextElementEnumerator(text);
        while(enumerator.MoveNext())
        {
            var cluster = enumerator.Current.ToString();

            if(string.IsNullOrEmpty(cluster))
                continue;

            var info = glyphCache.GetOrAdd(cluster, null);
            
            if(info.width != 0)
                glyphs.Add(info);
            else
                glyphs.Add(invalidGlyph);
        }

        return glyphs;
    }
}
