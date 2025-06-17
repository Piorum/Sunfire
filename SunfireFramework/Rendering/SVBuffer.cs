namespace SunfireFramework.Rendering;

public class SVBuffer(int width, int height)
{
    private readonly SVCell[] cells = new SVCell[width * height];
    public readonly int Width = width;
    public readonly int Height = height;

    public ref SVCell this[int x, int y]
    {
        get => ref cells[y * Width + x];
    }

    public ReadOnlySpan<SVCell> AsSpan() => cells.AsSpan();

    public void Clear() => Array.Fill(cells, SVCell.Blank);
}
