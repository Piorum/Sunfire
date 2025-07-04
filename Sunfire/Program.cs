using Sunfire.Enums;
using Sunfire.Registries;
using Sunfire.Tui;
using Sunfire.Input;
using Sunfire.Input.Models;
using Sunfire.Logging;
using Sunfire.Logging.Sinks;
using Sunfire.Logging.Models;
using Sunfire.FSUtils;
using Sunfire.FSUtils.Models;

namespace Sunfire;

internal class Program
{
    public static InputHandler<InputContext> InputHandler = new();
    public static Renderer Renderer = new(SVRegistry.GetRootSV());

    public static readonly CancellationTokenSource _cts = new();

    public static async Task Main(string[] args)
    {
        await SetupLogging([.. args]);

        var inputTask = StartInput();
        var renderTask = StartRender();
        await Task.WhenAll(inputTask, renderTask);

        await Logger.StopAndFlush();
    }

    private static async Task SetupLogging(HashSet<string> args)
    {
        bool debug = args.Contains("-D") || args.Contains("--debug");
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
    }

    private static async Task StartInput()
    {
        InputHandler.Context.Add(InputContext.Global);

        await InputHandler.CreateBinding()
            .AsIndifferent()
            .WithSequence(Key.KeyboardBind(ConsoleKey.Q))
            .WithContext([InputContext.Global])
            .WithBind(async (inputData) => { await InputHandler.Stop(); })
            .RegisterBind();

        await InputHandler.CreateBinding()
            .AsIndifferent()
            .WithSequence(Key.KeyboardBind(ConsoleKey.R, Input.Enums.Modifier.Ctrl | Input.Enums.Modifier.Alt))
            .WithContext([InputContext.Global])
            .WithBind(async (inputData) => { await Renderer.EnqueueAction(Renderer.RootView.Invalidate); })
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
    }

    private static async Task StartRender()
    {
        var renderLoopTask = Renderer.Start(_cts.Token);

        var tlLabel = SVRegistry.GetTopLeftLabel();
        await Renderer.EnqueueAction(async () =>
        {
            tlLabel.Text = "Top Label";
            await tlLabel.Invalidate();
        });

        var blLabel = SVRegistry.GetBottomLabel();
        await Renderer.EnqueueAction(async () =>
        {
            blLabel.Text = "Bottom Label";
            await blLabel.Invalidate();
        });

        var list = SVRegistry.GetCurrentList();
        var fsService = new FSService();
        var userProfleDir = await fsService.GetEntryAsync(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));
        if (userProfleDir is null || userProfleDir is not FSDirectory cwd) throw new("User profile is not a directory");

        var info = (await cwd.GetChildrenAsync())
            .OrderByDescending(e => e is FSDirectory)
            .ThenByDescending(e => e.IsHidden)
            .ThenBy(e => e.Name.ToLowerInvariant());

        foreach (var entry in info)
        {
            if (entry is FSDirectory dir)
            {
                await list.AddLabel(new()
                {
                    TextProperties = Ansi.Models.SAnsiProperty.Bold,
                    Text = dir.Name
                });
            }
            else if (entry is FSFile file)
            {
                await list.AddLabel(new()
                {
                    Text = file.Name
                });
            }
        }
        await Renderer.EnqueueAction(list.Invalidate);

        await renderLoopTask;
    }
}