namespace SunfireFramework.Views;

public class RootSV(int? sizeX = null, int? sizeY = null) : ISunfireView
{
    public int OriginX { set; get; } = 0;
    public int OriginY { set; get; } = 0;
    public int SizeX { set; get; } = sizeX ?? Console.BufferWidth;
    public int SizeY { set; get; } = sizeY ?? Console.BufferHeight;

    public ConsoleColor BackgroundColor { get; set; } = ConsoleColor.Black;

    required public PaneSV RootPane;

    public async Task Arrange()
    {
        RootPane.OriginX = 0;
        RootPane.OriginY = 0;

        RootPane.SizeX = SizeX;
        RootPane.SizeY = SizeY;

        await RootPane.Arrange();
    }

    public async Task Draw()
    {
        await RootPane.Draw();
    }

    public async Task ReSize()
    {
        SizeX = Console.BufferWidth;
        SizeY = Console.BufferHeight;

        await Arrange();
        await Draw();
    }
}
