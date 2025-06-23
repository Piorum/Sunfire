using Sunfire.Enums;
using Sunfire.Registries;
using Sunfire.Tui;
using Sunfire.Input;
using Sunfire.Input.Models;
using Sunfire.Logging;
using Sunfire.Logging.Models;

namespace Sunfire;

internal class Program
{
    public static InputHandler<InputContext> InputHandler = new();
    public static Renderer Renderer = new(SVRegistry.GetRootSV());

    public static readonly CancellationTokenSource _cts = new();

    public static async Task Main(string[] args)
    {
        bool debug = args.Contains("--debug");
        bool info = args.Contains("--info");
        bool warn = args.Contains("--warn");
        bool console = args.Contains("-C") || args.Contains("--console");

        List<LogLevel> logLevels = [LogLevel.Error, LogLevel.Fatal];

        if (debug)
            logLevels.AddRange([LogLevel.Debug, LogLevel.Info, LogLevel.Warn]);
        else if (info)
            logLevels.AddRange([LogLevel.Info, LogLevel.Warn]);
        else if (warn)
            logLevels.Add(LogLevel.Warn);

        if (console)
            await Logger.AddSink(new(new BufferSink(), [.. logLevels]));

        //Add file sink to store logs
        //await Logger.AddSink(new(new FileSink(), [.. logLevels]));

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
                    TextProperties = i < 5 ? Ansi.Models.SAnsiProperty.Bold : Ansi.Models.SAnsiProperty.None,
                    Text = $"{i}"
                });
            }
            await Renderer.EnqueueAction(list.Invalidate);

            await renderLoopTask;
        });

        await Logger.Info(nameof(Sunfire), "Input and Render Tasks Started.");
        await Task.WhenAll(inputTask, renderTask);
        await Logger.Info(nameof(Sunfire), "Shutting Down.");

        await Logger.StopAndFlush();
    }
}