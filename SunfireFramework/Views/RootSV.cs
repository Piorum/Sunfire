using SunfireFramework.Terminal;

namespace SunfireFramework.Views;

[System.Runtime.Versioning.SupportedOSPlatform("linux")]
[System.Runtime.Versioning.SupportedOSPlatform("macOS")]
[System.Runtime.Versioning.SupportedOSPlatform("windows")]
public class RootSV(int? sizeX = null, int? sizeY = null) : ISunfireView
{
    public int OriginX { set; get; } = 0;
    public int OriginY { set; get; } = 0;
    public int SizeX { set; get; } = sizeX ?? Console.BufferWidth;
    public int SizeY { set; get; } = sizeY ?? Console.BufferHeight;

    public ConsoleColor BackgroundColor { get; set; } = ConsoleColor.Black;

    required public PaneSV RootPane;
    private readonly IWindowResizer windowResizer = WindowResizerFactory.Create();

    public async Task Arrange()
    {
        await windowResizer.RegisterResizeEvent(this);

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
