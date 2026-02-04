using Wcwidth;
using Sunfire.Glyph.Models;
using Sunfire.Shared;

namespace Sunfire.Glyph;

public class GlyphCache : IdIndexedCache<string , byte?, GlyphData, (int id, byte width)>
{
    protected override GlyphData CreateObject(string cluster, byte? overrideWidth){
        var realWidth = MeasureGramphemeCluster(cluster);
        GlyphData newData = new() { GraphemeCluster = cluster, RealWidth = realWidth, Width = overrideWidth is null ? realWidth : overrideWidth.Value};

        return newData;
    }

    protected override (int id, byte width) CreateInfo(int id, GlyphData dataOjbect)
    {
        return (id, dataOjbect.Width);
    }

    protected override GlyphData Update(GlyphData dataObject, string cluster, byte? overrideWidth)
    {
        if(overrideWidth is not null)
            return dataObject with { Width = overrideWidth.Value };

        return dataObject;
    }

    private static byte MeasureGramphemeCluster(string graphemeCluster) =>
        (byte)graphemeCluster.EnumerateRunes().Sum(r => UnicodeCalculator.GetWidth(r));
}
