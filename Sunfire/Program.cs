using Sunfire.Core;
using SunfireInputParser;
using SunfireFramework.Terminal;
using Sunfire.Enums;
using SunfireInputParser.Builders;
using SunfireInputParser.Types;

namespace Sunfire;

internal class Program
{
    public static readonly CancellationTokenSource _cts = new();

    public static async Task Main()
    {
        var inputTask = Task.Run(async () =>
        {
            var inputHandler = new InputHandler<InputContext>();
            inputHandler.Context.Add(InputContext.Global);

            KeybindBuilder<InputContext> keybindBuilder = new();
            await keybindBuilder
                .WithSequence(Key.KeyboardBind(ConsoleKey.Q))
                .WithContext([InputContext.Global])
                .WithBind((inputData) => { _cts.Cancel(); return Task.CompletedTask; })
                .RegisterBind(inputHandler);

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