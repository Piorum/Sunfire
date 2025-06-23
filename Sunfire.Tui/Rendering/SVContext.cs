namespace Sunfire.Tui.Rendering;

public readonly struct SVContext(int originX, int originY, SVBuffer buffer)
{
    public readonly SVBuffer Buffer = buffer;

    public readonly int OriginX = originX;
    public readonly int OriginY = originY;

    public ref SVCell this[int x, int y]
    {
        get => ref Buffer[OriginX + x, OriginY + y];
    }
}
