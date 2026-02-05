namespace Sunfire.Tui.Models;

public class SVBuffer(int width, int height)
{
    private readonly SVCell[] cells = new SVCell[width * height];
    public int Width { private set; get; } = width;
    public int Height { private set; get; } = height;

    public ref SVCell this[int x, int y]
    {
        get => ref cells[y * Width + x];
    }

    public void Clear((int x, int y, int w, int h) area)
    {
        for(int i = area.y; i < area.y + area.h; i++)
            Array.Clear(cells, i * Width + area.x, area.w);
    }

    public void Clear() =>
        Array.Clear(cells);
}
