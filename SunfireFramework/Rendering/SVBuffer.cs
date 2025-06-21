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

    public void Resize(int width, int height)
    {
        if (width == Width && height == Height)
            return;

        var newCells = new SVCell[width * height];

        var copyWidth = Math.Min(width, Width);
        var copyHeight = Math.Min(height, Height);

        var oldSpan = cells.AsSpan();
        var newSpan = newCells.AsSpan();

        for (int y = 0; y < copyHeight; y++)
        {
            var sourceSlice = oldSpan.Slice(y * Width, copyWidth);
            var destSlice = newSpan.Slice(y * width, copyWidth);
            sourceSlice.CopyTo(destSlice);
        }

        cells = newCells;
        Width = width;
        Height = height;
    }
}
