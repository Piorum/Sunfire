namespace Sunfire.Glyph.Models;

public readonly struct GlyphData
{
    public string GraphemeCluster { get; init; }
    public byte Width { get; init; }
    public byte RealWidth { get; init; }
}
