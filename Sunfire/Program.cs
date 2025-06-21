using Sunfire.Enums;
using Sunfire.Registries;
using SunfireFramework;
using SunfireFramework.Terminal;
using SunfireInputParser;
using SunfireInputParser.Types;

namespace Sunfire;

internal class Program
{
    public static InputHandler<InputContext> InputHandler = new();
    public static Renderer Renderer = new(SVRegistry.GetRootSV());

    public static readonly CancellationTokenSource _cts = new();

    public static async Task Main()
    {
        var inputTask = Task.Run(async () =>
        {
            InputHandler.Context.Add(InputContext.Global);

            await InputHandler.CreateBinding()
                .AsIndifferent()
                .WithSequence(Key.KeyboardBind(ConsoleKey.Q))
                .WithContext([InputContext.Global])
                .WithBind(async (inputData) => { await InputHandler.Stop(); })
                .RegisterBind();

            var currentList = SVRegistry.GetCurrentList();
            await InputHandler.CreateBinding()
                .AsIndifferent()
                .WithSequence(Key.KeyboardBind(ConsoleKey.W))
                .WithContext([InputContext.Global])
                .WithBind(async (inputData) =>
                {
                    if (currentList.SelectedIndex > 0)
                        await Renderer.EnqueueAction(async () =>
                        { 
                            currentList.SelectedIndex--;
                            await currentList.Invalidate();
                        });
                })
                .RegisterBind();
            await InputHandler.CreateBinding()
                .AsIndifferent()
                .WithSequence(Key.KeyboardBind(ConsoleKey.S))
                .WithContext([InputContext.Global])
                .WithBind(async (inputData) =>
                {
                    if (currentList.SelectedIndex < currentList.MaxIndex)
                        await Renderer.EnqueueAction(async () =>
                        { 
                            currentList.SelectedIndex++;
                            await currentList.Invalidate();
                        });
                })
                .RegisterBind();

            await InputHandler.Start(_cts);
        });

        Console.CursorVisible = false;
        Console.Clear();

        var renderTask = Task.Run(async () =>
        {
            var renderLoopTask = Renderer.Start(_cts.Token);

            var tlLabel = SVRegistry.GetTopLeftLabel();
            await Renderer.EnqueueAction(async () => {
                tlLabel.Text = "Top Label";
                await tlLabel.Invalidate();
            });

            var blLabel = SVRegistry.GetBottomLabel();
            await Renderer.EnqueueAction(async () => {
                blLabel.Text = "Bottom Label";
                await blLabel.Invalidate();
            });

            var list = SVRegistry.GetCurrentList();
            for (int i = 0; i < 10; i++)
            {
                await list.AddLabel(new()
                {
                    Text = $"{i}"
                });
            }
            await Renderer.EnqueueAction(list.Invalidate);

            await renderLoopTask;
        });

        await Task.WhenAll(inputTask, renderTask);

        Console.Clear();
        await SVLogger.OutputLog();
    }
}