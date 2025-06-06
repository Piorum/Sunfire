using SunfireFramework.Views;

namespace SunfireFramework.Terminal.Platforms;

[System.Runtime.Versioning.SupportedOSPlatform("linux")]
public class WindowsWindowResizer : IWindowResizer
{
    public Task RegisterResizeEvent(RootSV root)
    {
        throw new NotImplementedException();
    }
}
