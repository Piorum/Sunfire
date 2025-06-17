using SunfireFramework.Views;

namespace SunfireFramework.Terminal.Platforms;

[System.Runtime.Versioning.SupportedOSPlatform("windows")]
public class WindowsWindowResizer : IWindowResizer
{
    public bool Registered { get; set; } = false;
    public Task RegisterResizeEvent(RootSV root)
    {
        throw new NotImplementedException();
    }
}
