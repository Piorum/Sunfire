using SunfireFramework.Terminal.Platforms;
using SunfireFramework.Views;

namespace SunfireFramework.Terminal;

public interface IWindowResizer
{
    Task RegisterResizeEvent(RootSV root);
    bool Registered { get; set; }
}

internal static class WindowResizerFactory
{
    public static IWindowResizer Create()
    {
        if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS())
            return new UnixWindowResizer();
        else if (OperatingSystem.IsWindows())
            return new WindowsWindowResizer();

        throw new PlatformNotSupportedException();
    }
}