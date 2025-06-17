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
            var renderer = new Renderer(rootSv, 165);

            var renderLoopTask = renderer.Render(_cts.Token);

            var tlLabel = SVRegistry.GetTopLeftLabel();
            tlLabel.Text = "Top Label";
            await tlLabel.Arrange();

            var blLabel = SVRegistry.GetBottomLabel();
            blLabel.Text = "Bottom Label";
            await blLabel.Arrange();

            var list = SVRegistry.GetCurrentList();
            for (int i = 0; i < 10; i++)
            {
                await list.AddLabel(new()
                {
                    Text = $"{i}"
                });
            }
            await list.Arrange();

            await renderLoopTask;
        });

        await Task.WhenAll(inputTask, renderTask);

        Console.Clear();
        await TerminalWriter.OutputLog();
    }
}