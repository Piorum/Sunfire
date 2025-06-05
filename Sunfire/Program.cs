using Sunfire.Core;
using SunfireFramework;

namespace Sunfire;

[System.Runtime.Versioning.SupportedOSPlatform("linux")]
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
        await ConsoleWriter.OutputErrorLog();
    }
}