using Sunfire.Core;
using SunfireInputParser;
using SunfireFramework.Terminal;

namespace Sunfire;

internal class Program
{
    public static readonly CancellationTokenSource _cts = new();

    public static async Task Main()
    {
        var inputTask = Task.Run(async () =>
        {
            var inputHandler = new InputHandler();
            await inputHandler.Start(_cts);
        });

        Console.CursorVisible = false;
        Console.Clear();

        var renderTask = Task.Run(() => RenderHandler.RenderLoop(_cts.Token));

        await Task.WhenAll(inputTask, renderTask);

        Console.Clear();
        await TerminalWriter.OutputLog();
    }
}