using Sunfire.Core;
using SunfireFramework.Terminal;

namespace Sunfire;

[System.Runtime.Versioning.SupportedOSPlatform("linux")]
[System.Runtime.Versioning.SupportedOSPlatform("macOS")]
[System.Runtime.Versioning.SupportedOSPlatform("windows")]
internal class Program
{
    public static readonly CancellationTokenSource _cts = new();

    public static async Task Main()
    {
        var inputTask = Task.Run(() => InputHandler.InputLoop(_cts));

        Console.CursorVisible = false;
        Console.Clear();

        var renderTask = Task.Run(() => RenderHandler.RenderLoop(_cts.Token));

        await Task.WhenAll(inputTask, renderTask);

        Console.Clear();
        await TerminalWriter.OutputLog();
    }
}