namespace Sunfire.Glyph.Models;

public record GlyphData(
    byte[] GraphemeCluster,
    byte RealWidth,
    byte VisualWidth);
