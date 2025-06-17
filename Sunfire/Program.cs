using Sunfire.Enums;
using Sunfire.Registries;
using SunfireFramework;
using SunfireFramework.Terminal;
using SunfireInputParser;
using SunfireInputParser.Types;

namespace Sunfire;

internal class Program
{
    public static InputHandler<InputContext>? InputHandler;

    public static readonly CancellationTokenSource _cts = new();

    public static async Task Main()
    {
        var inputTask = Task.Run(async () =>
        {
            InputHandler = new InputHandler<InputContext>();
            InputHandler.Context.Add(InputContext.Global);

            await InputHandler.CreateBinding()
                .AsIndifferent()
                .WithSequence(Key.KeyboardBind(ConsoleKey.Q))
                .WithContext([InputContext.Global])
                .WithBind((inputData) => { _cts.Cancel(); return Task.CompletedTask; })
                .RegisterBind();

            await InputHandler.Start(_cts);
        });

        Console.CursorVisible = false;
        Console.Clear();

        var renderTask = Task.Run(async () =>
        {
            var rootSv = SVRegistry.GetRootSV();
            var renderer = new Renderer(rootSv);

            await renderer.Render(_cts.Token);
        });

        await Task.WhenAll(inputTask, renderTask);

        Console.Clear();
        await TerminalWriter.OutputLog();
    }
}