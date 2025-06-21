using System.Runtime.InteropServices;

namespace SunfireFramework.Terminal.Platforms;

[System.Runtime.Versioning.SupportedOSPlatform("linux")]
[System.Runtime.Versioning.SupportedOSPlatform("macOS")]
public class UnixWindowResizer : IWindowResizer
{
    //Static store for sigwinch registration so it doesn't get garbage collected
    private PosixSignalRegistration? sigwinchRegistration;
    public bool Registered { get; set; } = false;

    public Task RegisterResizeEvent(Renderer renderer)
    {
        sigwinchRegistration = PosixSignalRegistration.Create(PosixSignal.SIGWINCH, sig =>
        {
            Task.Run(async () =>
            {
                try
                {
                    //tight loop until buffer size is updated
                    //while (Console.BufferHeight == renderer.RootView.SizeY & Console.BufferWidth == renderer.RootView.SizeX) { }

                    //await SVLogger.LogMessage($"Test Log");
                    await renderer.Resize();
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"{ex}");
                }
            });
        });
        Registered = true;
        return Task.CompletedTask;
    }
}
