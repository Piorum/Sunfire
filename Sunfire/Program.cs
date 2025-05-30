using Sunfire.Views;
using Sunfire.Enums;
using System.Runtime.InteropServices;

namespace Sunfire;

[System.Runtime.Versioning.SupportedOSPlatform("linux")]
internal class Program
{
    public static readonly CancellationTokenSource _cts = new();
    public static readonly ManualResetEventSlim _renderSignal = new();

    public static AppState _appState = new();

    public static async Task Main()
    {
        Console.CursorVisible = false;
        Console.Clear();

        var inputTask = Task.Run(InputLoop);
        var renderTask = Task.Run(RenderLoop);

        await Task.WhenAll(inputTask, renderTask);

        Console.Clear();
    }

    private static Task InputLoop()
    {
        while (!_cts.Token.IsCancellationRequested)
        {
            var keyInfo = Console.ReadKey(true);
            if (Keybindings.Equals(keyInfo, Keybindings.ExitKey))
                _cts.Cancel();

            if (Keybindings.Equals(keyInfo, Keybindings.NavUp))
                _appState.UpdateTopLabel("up");
            if (Keybindings.Equals(keyInfo, Keybindings.NavOut))
                _appState.UpdateTopLabel("out");
            if (Keybindings.Equals(keyInfo, Keybindings.NavDown))
                _appState.UpdateTopLabel("down");
            if (Keybindings.Equals(keyInfo, Keybindings.NavIn))
                _appState.UpdateTopLabel("in");
        }
        return Task.CompletedTask;
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
        PosixSignalRegistration.Create(PosixSignal.SIGWINCH, sig =>
        {
            Task.Run(async () =>
            {
                await TUIRenderer.ExecuteRenderAction(_appState.RootView, RenderAction.Arrange);
            });
        });
        return Task.CompletedTask;
    }

}