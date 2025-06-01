using Sunfire.Enums;
using SunfireFramework.TextBoxes;
using SunfireFramework.Views;
using System.Runtime.InteropServices;

namespace Sunfire;

[System.Runtime.Versioning.SupportedOSPlatform("linux")]
internal class Program
{
    public static readonly CancellationTokenSource _cts = new();
    public static readonly ManualResetEventSlim _renderSignal = new();
    public static PosixSignalRegistration? sigwinchRegistration;

    public static AppState _appState = new();

    public static async Task Main()
    {
        Console.CursorVisible = false;
        Console.Clear();
        
        ListSV testList = new();

        for (int i = 0; i < 10; i++)
        {
            testList.Labels.Add(new()
            {
                TextFields =
                [
                    new TextField()
                    {
                        Text = $"{i}"
                    }
                ]
            });
        }

        RootSV rootSV = new()
        {
            RootPane = new()
            {
                SubViews =
                [
                    testList
                ]
            }
        };

        await rootSV.Arrange();
        await rootSV.Draw();

        await Task.Delay(-1);

        var inputTask = Task.Run(InputLoop);
        var renderTask = Task.Run(RenderLoop);

        await Task.WhenAll(inputTask, renderTask);

        Console.Clear();
    }

    private async static Task InputLoop()
    {
        while (!_cts.Token.IsCancellationRequested)
        {
            var keyInfo = Console.ReadKey(true);
            if (Keybindings.Equals(keyInfo, Keybindings.ExitKey))
                _cts.Cancel();

            if (Keybindings.Equals(keyInfo, Keybindings.NavUp))
                await _appState.MoveUp();
            if (Keybindings.Equals(keyInfo, Keybindings.NavOut))
                await _appState.UpdateTopLabel("out");
            if (Keybindings.Equals(keyInfo, Keybindings.NavDown))
                await _appState.MoveDown();
            /*if (Keybindings.Equals(keyInfo, Keybindings.NavIn))
                await _appState.UpdateTopLabel("in");*/
            if (Keybindings.Equals(keyInfo, Keybindings.NavTop))
                await _appState.MoveTop();
            if (Keybindings.Equals(keyInfo, Keybindings.NavBottom))
                await _appState.MoveBottom();

            if (Keybindings.Equals(keyInfo, Keybindings.NavIn))
                await _appState.Add([new() { Text = "Test" }]);

            if (Keybindings.Equals(keyInfo, Keybindings.Reload))
                await TUIRenderer.ExecuteRenderAction(_appState.RootView, RenderAction.Arrange);

            if (Keybindings.Equals(keyInfo, Keybindings.Select))
                await _appState.Select();

            if (Keybindings.Equals(keyInfo, Keybindings.ForceDelete))
                await _appState.Delete();
        }
    }

    private static async Task RenderLoop()
    {
        await RegisterResizeEvent();
        await TUIRenderer.ExecuteRenderAction(_appState.RootView, RenderAction.Arrange);

        while (!_cts.Token.IsCancellationRequested)
        {
            try
            {
                _renderSignal.Wait(_cts.Token);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            //await TUIRenderer.ExecuteRenderTasks(_appState.RootView);

            _renderSignal.Reset();
        }
        return;
    }

    private static Task RegisterResizeEvent()
    {
        sigwinchRegistration = PosixSignalRegistration.Create(PosixSignal.SIGWINCH, sig =>
        {
            Task.Run(async () =>
            {
                try
                {
                    await TUIRenderer.ExecuteRenderAction(_appState.RootView, RenderAction.Arrange);
                }
                catch ( Exception ex )
                {
                    Console.Error.WriteLine($"{ex}");
                }
            });
        });
        return Task.CompletedTask;
    }

}