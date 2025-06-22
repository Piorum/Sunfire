namespace SunfireFramework.Rendering;

public class SVBuffer(int width, int height)
{
    private SVCell[] cells = new SVCell[width * height];
    public int Width { private set; get; } = width;
    public int Height { private set; get; } = height;

    public ref SVCell this[int x, int y]
    {
        get => ref cells[y * Width + x];
    }

    public Span<SVCell> AsSpan() => cells.AsSpan();

    public void Clear() => Array.Fill(cells, SVCell.Blank);
}
