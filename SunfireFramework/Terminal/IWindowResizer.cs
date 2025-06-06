using SunfireFramework.Terminal.Platforms;
using SunfireFramework.Views;

namespace SunfireFramework.Terminal;

public interface IWindowResizer
{
    Task RegisterResizeEvent(RootSV root);
}

[System.Runtime.Versioning.SupportedOSPlatform("linux")]
[System.Runtime.Versioning.SupportedOSPlatform("macOS")]
[System.Runtime.Versioning.SupportedOSPlatform("windows")]
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