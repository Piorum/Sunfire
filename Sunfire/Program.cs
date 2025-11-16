using Sunfire.Enums;
using Sunfire.Registries;
using Sunfire.Tui;
using Sunfire.Input;
using Sunfire.Input.Models;
using Sunfire.Logging;
using Sunfire.Logging.Sinks;
using Sunfire.Logging.Models;

namespace Sunfire;

internal class Program
{
    public static InputHandler<InputContext> InputHandler = new();
    public static Renderer Renderer = new(SVRegistry.RootSV);

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

        //Exit
        await InputHandler.CreateBinding()
            .AsIndifferent()
            .WithSequence(Key.KeyboardBind(ConsoleKey.Q))
            .WithContext([InputContext.Global])
            .WithBind(async (inputData) => { await InputHandler.Stop(); })
            .RegisterBind();

        //Rerender
        await InputHandler.CreateBinding()
            .AsIndifferent()
            .WithSequence(Key.KeyboardBind(ConsoleKey.R, Input.Enums.Modifier.Ctrl | Input.Enums.Modifier.Alt))
            .WithContext([InputContext.Global])
            .WithBind(async (inputData) => { await Renderer.EnqueueAction(Renderer.RootView.Invalidate); })
            .RegisterBind();

        //Nav
        await InputHandler.CreateBinding()
            .AsIndifferent()
            .WithSequence(Key.KeyboardBind(ConsoleKey.W))
            .WithContext([InputContext.Global])
            .WithBind(async (inputData) => await AppState.NavUp())
            .RegisterBind();
        await InputHandler.CreateBinding()
            .AsIndifferent()
            .WithSequence(Key.KeyboardBind(ConsoleKey.S))
            .WithContext([InputContext.Global])
            .WithBind(async (inputData) => await AppState.NavDown())
            .RegisterBind();
        await InputHandler.CreateBinding()
            .AsIndifferent()
            .WithSequence(Key.KeyboardBind(ConsoleKey.A))
            .WithContext([InputContext.Global])
            .WithBind(async (inputData) => await AppState.NavOut())
            .RegisterBind();
        await InputHandler.CreateBinding()
            .AsIndifferent()
            .WithSequence(Key.KeyboardBind(ConsoleKey.D))
            .WithContext([InputContext.Global])
            .WithBind(async (inputData) => await AppState.NavIn())
            .RegisterBind();

        await InputHandler.Start(_cts);
    }

    private static async Task StartRender()
    {
        var renderLoopTask = Renderer.Start(_cts.Token);

        await AppState.Init();

        await renderLoopTask;
    }
}