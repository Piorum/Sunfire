using Sunfire.Core;
using SunfireInputParser;
using SunfireFramework.Terminal;
using Sunfire.Enums;
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
                .WithSequence(Key.KeyboardBind(ConsoleKey.Q, SunfireInputParser.Enums.Modifier.Ctrl))
                .WithContext([InputContext.Global])
                .WithBind((inputData) => { _cts.Cancel(); return Task.CompletedTask; })
                .RegisterBind();

            await InputHandler.Start(_cts);
        });

        Console.CursorVisible = false;
        Console.Clear();

        var renderTask = Task.Run(() => RenderHandler.Start(_cts.Token));

        await Task.WhenAll(inputTask, renderTask);

        Console.Clear();
        await TerminalWriter.OutputLog();
    }
}