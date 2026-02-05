using System.Runtime.InteropServices;
using Sunfire.Ansi;
using Sunfire.Ansi.Models;
using Sunfire.Glyph;

namespace Sunfire.Tui.Models;

[StructLayout(LayoutKind.Sequential, Size = 8)]
public readonly struct SVCell(ulong value)
{
    public readonly ulong Value = value;
    
    private const int GlyphIdBits = 24;
    private const int WidthBits = 8;

    private const ulong glyphIdMask = (1UL << GlyphIdBits) - 1;
    private const ulong widthMask = (1UL << WidthBits) - 1;

    public readonly int GlyphId => (int)(Value & glyphIdMask);
    public readonly byte Width => (byte)((Value >> GlyphIdBits) & widthMask);
    public readonly int StyleId => (int)(Value >> (GlyphIdBits + WidthBits));

    public SVCell(int glyphId, byte width, int styleId) : this(((ulong)(uint)styleId << 32) | ((ulong)width << 24) | (uint)glyphId) { }

    public bool Equals(SVCell other) => Value == other.Value;
    public override bool Equals(object? obj) => obj is SVCell other && Equals(other);
    public override int GetHashCode() => Value.GetHashCode();
    public static bool operator ==(SVCell left, SVCell right) => left.Value == right.Value;
    public static bool operator !=(SVCell left, SVCell right) => left.Value != right.Value;

    public static readonly SVCell Blank;

    static SVCell()
    {
        var (id, width) = GlyphFactory.GetGlyphIds(" ").First();
        var styleId = StyleFactory.GetStyleId((null, null, SAnsiProperty.None));

        Blank = new(id, width, styleId);
    }
}
