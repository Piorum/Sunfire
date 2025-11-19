using Sunfire.Enums;
using Sunfire.Registries;
using Sunfire.Tui;
using Sunfire.Input;
using Sunfire.Input.Models;
using Sunfire.Logging;
using Sunfire.Logging.Sinks;
using Sunfire.Logging.Models;
using System.Collections.ObjectModel;

namespace Sunfire;

internal class Program
{
    public static InputHandler<InputContext> InputHandler = new();
    public static Renderer Renderer = new(SVRegistry.RootSV);
    public static AppOptions Options = new();

    public static readonly CancellationTokenSource _cts = new();

    public static async Task Main(string[] args)
    {
        var argsHS = args.ToHashSet();
        if(argsHS.Contains("-D") || argsHS.Contains("--debug"))
            Options.DebugLogs = true;
        if(argsHS.Contains("--info"))
            Options.InfoLogs = true;
        if(argsHS.Contains("--warn"))
            Options.WarnLogs = true;
        if(argsHS.Contains("-C") || argsHS.Contains("--console"))
            Options.OutputLogsToConsole = true;
        if(argsHS.Contains("-U") || argsHS.Contains("--user"))
            Options.UseUserProfileAsDefault = true;

        await InitLogging();

        var inputTask = Input();
        var renderTask = Render();

        try
        {
            var first = await Task.WhenAny(inputTask, renderTask);

            if(first.IsFaulted)
                await Task.FromException(first.Exception);
        }
        catch
        {
            await Stop();
        }
        finally
        {
            try
            {
                await Task.WhenAll(inputTask, renderTask);
            }
            catch (Exception ex)
            {
                var exs = ex is AggregateException ae ? ae.InnerExceptions : new AggregateException(ex).InnerExceptions;
                foreach(var ie in exs)
                    await Logger.Error(nameof(Sunfire), $"Major Exception:\n{ex}");
            }

            await Logger.StopAndFlush();
        }
    }

    private static async Task Stop()
    {
        if (_cts is not null)
        {
            await _cts.CancelAsync();
            _cts.Dispose();
        }
    }

    private static async Task InitLogging()
    {

        List<LogLevel> logLevels = [LogLevel.Error, LogLevel.Fatal];

        if (Options.DebugLogs)
            logLevels.AddRange([LogLevel.Debug, LogLevel.Info, LogLevel.Warn]);
        else if (Options.InfoLogs)
            logLevels.AddRange([LogLevel.Info, LogLevel.Warn]);
        else if (Options.WarnLogs)
            logLevels.Add(LogLevel.Warn);

        if (Options.OutputLogsToConsole)
            await Logger.AddSink(new(new BufferSink(), [.. logLevels]));

        //Add file sink to store logs
        //await Logger.AddSink(new(new FileSink(), [.. logLevels]));
    }

    private static async Task Input()
    {
        InputHandler.Context.Add(InputContext.Global);

        //Exit
        await InputHandler.CreateBinding()
            .AsIndifferent()
            .WithSequence(Key.KeyboardBind(ConsoleKey.Q))
            .WithContext([InputContext.Global])
            .WithBind(async (inputData) => { await Stop(); })
            .RegisterBind();

        //Try Redraw
        await InputHandler.CreateBinding()
            .AsIndifferent()
            .WithSequence(Key.KeyboardBind(ConsoleKey.R, Sunfire.Input.Enums.Modifier.Ctrl | Sunfire.Input.Enums.Modifier.Alt))
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

        //Nav Ext
        //Jump Top
        await InputHandler.CreateBinding()
            .AsIndifferent()
            .WithSequence(Key.KeyboardBind(ConsoleKey.G))
            .WithContext([InputContext.Global])
            .WithBind(async (inputData) => await AppState.NavList(-SVRegistry.CurrentList.SelectedIndex))
            .RegisterBind();
        //Jump Bottom
        await InputHandler.CreateBinding()
            .AsIndifferent()
            .WithSequence(Key.KeyboardBind(ConsoleKey.G, Sunfire.Input.Enums.Modifier.Shift))
            .WithContext([InputContext.Global])
            .WithBind(async (inputData) => await AppState.NavList(SVRegistry.CurrentList.MaxIndex - SVRegistry.CurrentList.SelectedIndex))
            .RegisterBind();
        
        //Toggles
        await InputHandler.CreateBinding()
            .WithSequence(Key.KeyboardBind(ConsoleKey.Z))
            .WithSequence(Key.KeyboardBind(ConsoleKey.H))
            .WithContext([InputContext.Global])
            .WithBind(async (inputData) => await AppState.ToggleHidden())
            .RegisterBind();

        await InputHandler.Init(_cts.Token);
    }

    private static async Task Render()
    {
        var renderLoopTask = Renderer.Start(_cts.Token);

        await AppState.Init();

        await renderLoopTask;
    }
}