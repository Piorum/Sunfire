namespace Sunfire.Tui.Models;

/// <summary>
/// Minimal context struct that shifts the origin, does not contain bounds checking.
/// </summary>
/// <param name="originX">X coordinate of new origin.</param>
/// <param name="originY">Y coordinate of new origin.</param>
/// <param name="buffer">Buffer that data will be referenced from.</param>
public readonly struct SVContext(int originX, int originY, SVBuffer buffer)
{
    public readonly SVBuffer Buffer = buffer;

    public readonly int OriginX = originX;
    public readonly int OriginY = originY;

    /// <summary></summary>
    /// <returns>Cell at (x,y) relative to the top left origin (OriginX,OriginY)</returns>
    public ref SVCell this[int x, int y]
    {
        get => ref Buffer[OriginX + x, OriginY + y];
    }
}
