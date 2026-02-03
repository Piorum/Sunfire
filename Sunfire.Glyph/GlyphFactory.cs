using System.Globalization;
using Sunfire.Glyph.Models;

namespace Sunfire.Glyph;

public static class GlyphFactory
{
    private readonly static GlyphInfo invalidGlyph = GetGlyphs("\uFFFD").First();

    public static List<GlyphInfo> GetGlyphs(string text)
    {
        List<GlyphInfo> glyphs = [];

        var enumerator = StringInfo.GetTextElementEnumerator(text);
        while(enumerator.MoveNext())
        {
            var cluster = enumerator.Current.ToString();

            if(string.IsNullOrEmpty(cluster))
                continue;

            var info = GlyphLibrary.GetOrAdd(cluster);
            glyphs.Add(info.Width != 0 ? info : invalidGlyph);
        }

        return glyphs;
    }
}
