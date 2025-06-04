using SunfireFramework.Enums;
using System.Runtime.InteropServices;
using Sunfire.Factories;
using System.Diagnostics;
using SunfireFramework;

namespace Sunfire;

[System.Runtime.Versioning.SupportedOSPlatform("linux")]
internal class Program
{
    public static readonly CancellationTokenSource _cts = new();
    public static readonly ManualResetEventSlim _renderSignal = new();
    public static PosixSignalRegistration? sigwinchRegistration;

    public static async Task Main()
    {
        var inputTask = Task.Run(InputLoop);

        var setupTask = Task.Run(() =>
        {
            Console.CursorVisible = false;
            Console.Clear();
        });

        await setupTask;

        var renderTask = Task.Run(RenderLoop);
        await Task.WhenAll(inputTask, renderTask);

        Console.Clear();
        await ConsoleWriter.OutputErrorLog();
    }

    private static Task InputLoop()
    {
        while (!_cts.Token.IsCancellationRequested)
        {
            var keyInfo = Console.ReadKey(true);
            if (Keybindings.Equals(keyInfo, Keybindings.ExitKey))
                _cts.Cancel();

            /*if (Keybindings.Equals(keyInfo, Keybindings.NavUp))
                await _appState.MoveUp();
            if (Keybindings.Equals(keyInfo, Keybindings.NavOut))
                await _appState.UpdateTopLabel("out");
            if (Keybindings.Equals(keyInfo, Keybindings.NavDown))
                await _appState.MoveDown();
            if (Keybindings.Equals(keyInfo, Keybindings.NavIn))
                await _appState.UpdateTopLabel("in");
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
                await _appState.Delete();*/
        }
        return Task.CompletedTask;
    }

    private static async Task RenderLoop()
    {
        await RegisterResizeEvent();

        //Initial Draw
        var rootSv = SVFactory.GetRootSV();
        await rootSv.Arrange();
        await rootSv.Draw();
        await PopulateFields();

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
                    var rootSV = SVFactory.GetRootSV();

                    //tight loop until buffer size is updated
                    while (Console.BufferHeight == rootSV.SizeY & Console.BufferWidth == rootSV.SizeX) { }

                    await rootSV.ReSize();
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"{ex}");
                }
            });
        });
        return Task.CompletedTask;
    }

    private static async Task PopulateFields()
    {
        var currentListTask = Task.Run(async () =>
        {
            var currentList = SVFactory.GetCurrentList();
            for (int i = 0; i < 10; i++)
            {
                await currentList.AddLabel(new()
                {
                    TextFields =
                    [
                        new()
                    {
                        Text = $"{i}"
                    }
                    ],
                    Properties =
                    [
                        TextProperty.Bold
                    ]
                });
            }

            await currentList.Arrange();
            await currentList.Draw();
        });

        var topLabelTask = Task.Run(async () =>
        {
            var topLabel = SVFactory.GetTopLabel();
            topLabel.TextFields.Add(new()
            {
                Text = "Top Label"
            });
            topLabel.Properties.Add(TextProperty.Bold);

            await topLabel.Arrange();
            await topLabel.Draw();
        });

        var bottomLabelTask = Task.Run(async () =>
        {
            var bottomLabel = SVFactory.GetBottomLabel();
            bottomLabel.TextFields.Add(new()
            {
                Text = "Bottom Label"
            });
            bottomLabel.Properties.Add(TextProperty.Bold);

            await bottomLabel.Arrange();
            await bottomLabel.Draw();
        });

        await Task.WhenAll(currentListTask, topLabelTask, bottomLabelTask);
    }

}