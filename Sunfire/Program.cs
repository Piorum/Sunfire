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
        _ = RegionCheck();

        var argsHS = args.ToHashSet();
        Options.DebugLogs = argsHS.Contains("-D") || argsHS.Contains("--debug");
        Options.InfoLogs = argsHS.Contains("--info");
        Options.WarnLogs = argsHS.Contains("--warn");
        Options.OutputLogsToConsole = argsHS.Contains("-C") || argsHS.Contains("--console");
        Options.UseUserProfileAsDefault = argsHS.Contains("-U") || argsHS.Contains("--user");

        await InitLogging();
        await Logger.Debug(nameof(Sunfire), "[Startup]");

        _ = Task.Run(async () => await AppState.Init());

        await RegisterBinds();
        await App.Run();
    }

    private static Task RegionCheck() =>
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
            await TuiApp.InitLogging(logLevels);

        //Add file sink to store logs
        //await Logger.AddSink(new(new FileSink(), [.. logLevels]));
    }

    private static async Task RegisterBinds()
    {
        List<KeybindBuilder> binds = [
            //Exit
            App.InputHandler.Bind()
                .WithKey(InputKey.KeyboardBind(ConsoleKey.Q))
                .WithBind(new (async (_) => await App.Stop())),

            //Try Reload
            App.InputHandler.Bind()
                .WithKey(InputKey.KeyboardBind(ConsoleKey.R, Moonfire.Input.Enums.InputModifier.Ctrl | Moonfire.Input.Enums.InputModifier.Alt))
                .WithBind(new(async (_) => 
                { 
                    await App.Renderer.EnqueueActionClear(SVRegistry.RootPane.OriginX, SVRegistry.RootPane.OriginY, SVRegistry.RootPane.SizeX, SVRegistry.RootPane.SizeY);
                    await App.Renderer.EnqueueAction(SVRegistry.RootPane.Invalidate);
                    await AppState.InvalidateState();
                })),

            //Nav
            App.InputHandler.Bind()
                .WithKey(InputKey.KeyboardBind(ConsoleKey.UpArrow))
                .WithBind(new(async (_)=> await AppState.NavUp())),
            App.InputHandler.Bind()
                .WithKey(InputKey.KeyboardBind(ConsoleKey.W))
                .WithBind(new(async (_)=> await AppState.NavUp())),
            App.InputHandler.Bind()
                .WithKey(InputKey.KeyboardBind(ConsoleKey.DownArrow))
                .WithBind(new(async (_)=> await AppState.NavDown())),
            App.InputHandler.Bind()
                .WithKey(InputKey.KeyboardBind(ConsoleKey.S))
                .WithBind(new(async (_)=> await AppState.NavDown())),
            App.InputHandler.Bind()
                .WithKey(InputKey.KeyboardBind(ConsoleKey.LeftArrow))
                .WithBind(new(async (_)=> await AppState.NavOut())),
            App.InputHandler.Bind()
                .WithKey(InputKey.KeyboardBind(ConsoleKey.A))
                .WithBind(new(async (_)=> await AppState.NavOut())),
            App.InputHandler.Bind()
                .WithKey(InputKey.KeyboardBind(ConsoleKey.RightArrow))
                .WithBind(new(async (_)=> await AppState.NavIn())),
            App.InputHandler.Bind()
                .WithKey(InputKey.KeyboardBind(ConsoleKey.D))
                .WithBind(new(async (_)=> await AppState.NavIn())),
            App.InputHandler.Bind()
                .WithKey(InputKey.KeyboardBind(ConsoleKey.Enter))
                .WithBind(new(async (_)=> await AppState.HandleFile())),

            //Nav Ext
            //Jump Top
            App.InputHandler.Bind()
                .WithKey(InputKey.KeyboardBind(ConsoleKey.G))
                .WithBind(new(async (_)=> await AppState.NavList(-SVRegistry.CurrentList.SelectedIndex))),
            //Jump Bottom
            App.InputHandler.Bind()
                .WithKey(InputKey.KeyboardBind(ConsoleKey.G, Moonfire.Input.Enums.InputModifier.Shift))
                .WithBind(new(async (_)=> await AppState.NavList(SVRegistry.CurrentList.MaxIndex - SVRegistry.CurrentList.SelectedIndex))),
            //Search
            App.InputHandler.Bind()
                .WithKey(InputKey.KeyboardBind(ConsoleKey.Divide))
                .WithBind(new(async (_)=> await AppState.Search())),
            
            //Toggles
            App.InputHandler.Bind()
                .WithKey(InputKey.KeyboardBind(ConsoleKey.Z))
                .WithKey(InputKey.KeyboardBind(ConsoleKey.H))
                .WithBind(new(async (_)=> await AppState.ToggleHidden())),

            //Editing Binds
            App.InputHandler.Bind()
                .WithKey(InputKey.KeyboardBind(ConsoleKey.Spacebar))
                .WithBind(new(async (_)=> await AppState.Tag())),
            App.InputHandler.Bind()
                .WithKey(InputKey.KeyboardBind(ConsoleKey.C))
                .WithBind(new(async (_)=> await AppState.ClearTags())),
            App.InputHandler.Bind()
                .WithKey(InputKey.KeyboardBind(ConsoleKey.OemPeriod))
                .WithBind(new(async (_)=> await AppState.Action())),

            //Shell
            App.InputHandler.Bind()
                .WithKey(InputKey.KeyboardBind(ConsoleKey.OemComma))
                .WithBind(new(async (_)=> await AppState.Sh())),
        ];
        
        foreach(var bind in binds)
            bind.Register();
    }
}