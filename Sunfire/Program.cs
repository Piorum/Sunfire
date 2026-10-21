using Sunfire.Registries;
using Moonfire.Input;
using Moonfire.Logging;
using Moonfire.Logging.Models;
using GeoBlocker;
using Moonfire.Tui;
using Moonfire.Input.Models;

namespace Sunfire;

internal class Program
{
    public static TuiApp App = new(SVRegistry.RootPane);
    public static AppOptions Options = new();

    public static async Task Main(string[] args)
    {
        var argsHS = args.ToHashSet();
        Options.DebugLogs = argsHS.Contains("-D") || argsHS.Contains("--debug");
        Options.InfoLogs = argsHS.Contains("--info");
        Options.WarnLogs = argsHS.Contains("--warn");
        Options.OutputLogsToConsole = argsHS.Contains("-C") || argsHS.Contains("--console");
        Options.UseUserProfileAsDefault = argsHS.Contains("-U") || argsHS.Contains("--user");

        InitLogging();
        Logger.Debug(nameof(Sunfire), "[Startup]");

        ThreadPool.QueueUserWorkItem(_ =>
        {
            try
            {
                RegionCheck();
            }
            catch (Exception ex)
            {
                Logger.Error(nameof(Sunfire), $"RegionCheck failed:\n{ex}");
            }
        });

        ThreadPool.QueueUserWorkItem(_ =>
        {
            try
            {
                AppState.Init();
            }
            catch (Exception ex)
            {
                Logger.Error(nameof(Sunfire), $"RegionCheck failed:\n{ex}");
            }
        });

        RegisterBinds();

        App.Run();
    }

    private static void RegionCheck() =>
        Task.Run(async () =>
        {
            try
            {
                using IpGeoBlocker gb = IpGeoBlocker.CaliforniaGB();
                await gb.EnforceAsync();
            }
            catch (RegionBlockedException ex)
            {
                Console.WriteLine(ex.Message);
                Environment.Exit(0);
            }
        });

    private static void InitLogging()
    {

        List<LogLevel> logLevels = [LogLevel.Error, LogLevel.Fatal];

        if (Options.DebugLogs)
            logLevels.AddRange([LogLevel.Debug, LogLevel.Info, LogLevel.Warn]);
        else if (Options.InfoLogs)
            logLevels.AddRange([LogLevel.Info, LogLevel.Warn]);
        else if (Options.WarnLogs)
            logLevels.Add(LogLevel.Warn);

        if (Options.OutputLogsToConsole)
            TuiApp.InitLogging(logLevels);

        //Add file sink to store logs
        //await Logger.AddSink(new(new FileSink(), [.. logLevels]));
    }

    private static void RegisterBinds()
    {
        List<KeybindBuilder> binds = [
            //Exit
            App.InputHandler.Bind()
                .WithKey(InputKey.KeyboardBind(ConsoleKey.Q))
                .WithBind(App.Stop),

            //Try Reload
            App.InputHandler.Bind()
                .WithKey(InputKey.KeyboardBind(ConsoleKey.R, Moonfire.Input.Enums.InputModifier.Ctrl | Moonfire.Input.Enums.InputModifier.Alt))
                .WithBind(() => 
                { 
                    App.Renderer.EnqueueActionClear(SVRegistry.RootPane.OriginX, SVRegistry.RootPane.OriginY, SVRegistry.RootPane.SizeX, SVRegistry.RootPane.SizeY);
                    App.Renderer.EnqueueAction(SVRegistry.RootPane.Invalidate);
                    AppState.InvalidateState();
                }),

            //Nav
            App.InputHandler.Bind()
                .WithKey(InputKey.KeyboardBind(ConsoleKey.UpArrow))
                .WithBind(AppState.NavUp),
            App.InputHandler.Bind()
                .WithKey(InputKey.KeyboardBind(ConsoleKey.W))
                .WithBind(AppState.NavUp),
            App.InputHandler.Bind()
                .WithKey(InputKey.KeyboardBind(ConsoleKey.DownArrow))
                .WithBind(AppState.NavDown),
            App.InputHandler.Bind()
                .WithKey(InputKey.KeyboardBind(ConsoleKey.S))
                .WithBind(AppState.NavDown),
            App.InputHandler.Bind()
                .WithKey(InputKey.KeyboardBind(ConsoleKey.LeftArrow))
                .WithBind(AppState.NavOut),
            App.InputHandler.Bind()
                .WithKey(InputKey.KeyboardBind(ConsoleKey.A))
                .WithBind(AppState.NavOut),
            App.InputHandler.Bind()
                .WithKey(InputKey.KeyboardBind(ConsoleKey.RightArrow))
                .WithBind(AppState.NavIn),
            App.InputHandler.Bind()
                .WithKey(InputKey.KeyboardBind(ConsoleKey.D))
                .WithBind(AppState.NavIn),
            App.InputHandler.Bind()
                .WithKey(InputKey.KeyboardBind(ConsoleKey.Enter))
                .WithBind(AppState.HandleFile),

            //Nav Ext
            //Jump Top
            App.InputHandler.Bind()
                .WithKey(InputKey.KeyboardBind(ConsoleKey.G))
                .WithBind(() => AppState.NavList(-SVRegistry.CurrentList.SelectedIndex)),
            //Jump Bottom
            App.InputHandler.Bind()
                .WithKey(InputKey.KeyboardBind(ConsoleKey.G, Moonfire.Input.Enums.InputModifier.Shift))
                .WithBind(() => AppState.NavList(SVRegistry.CurrentList.MaxIndex - SVRegistry.CurrentList.SelectedIndex)),
            //Search
            App.InputHandler.Bind()
                .WithKey(InputKey.KeyboardBind(ConsoleKey.Divide))
                .WithBind(AppState.Search),
            
            //Toggles
            App.InputHandler.Bind()
                .WithKey(InputKey.KeyboardBind(ConsoleKey.Z))
                .WithKey(InputKey.KeyboardBind(ConsoleKey.H))
                .WithBind(AppState.ToggleHidden),

            //Editing Binds
            App.InputHandler.Bind()
                .WithKey(InputKey.KeyboardBind(ConsoleKey.Spacebar))
                .WithBind(AppState.Tag),
            App.InputHandler.Bind()
                .WithKey(InputKey.KeyboardBind(ConsoleKey.C))
                .WithBind(AppState.ClearTags),
            App.InputHandler.Bind()
                .WithKey(InputKey.KeyboardBind(ConsoleKey.OemPeriod))
                .WithBind(AppState.Action),

            //Shell
            App.InputHandler.Bind()
                .WithKey(InputKey.KeyboardBind(ConsoleKey.OemComma))
                .WithBind(AppState.Sh),
        ];
        
        foreach(var bind in binds)
            bind.Register();
    }
}