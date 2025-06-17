using System.Runtime.InteropServices;
using SunfireFramework.Views;

namespace SunfireFramework.Terminal.Platforms;

[System.Runtime.Versioning.SupportedOSPlatform("linux")]
[System.Runtime.Versioning.SupportedOSPlatform("macOS")]
public class UnixWindowResizer : IWindowResizer
{
    //Static store for sigwinch registration so it doesn't get garbage collected
    private PosixSignalRegistration? sigwinchRegistration;
    public bool Registered { get; set; } = false;

    public Task RegisterResizeEvent(RootSV root)
    {
        sigwinchRegistration = PosixSignalRegistration.Create(PosixSignal.SIGWINCH, sig =>
        {
            Task.Run(async () =>
            {
                try
                {
                    //tight loop until buffer size is updated
                    while (Console.BufferHeight == root.SizeY & Console.BufferWidth == root.SizeX) { }

                    await root.ReSize();
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
