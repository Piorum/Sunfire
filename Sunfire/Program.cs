using Sunfire.Enums;
using SunfireFramework.TextBoxes;
using SunfireFramework.Views;
using SunfireFramework.Enums;
using System.Diagnostics;
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
        var inputTask = Task.Run(InputLoop);

        var setupTask = Task.Run(() =>
        {
            Console.CursorVisible = false;
            Console.Clear();
        });

        ListSV currentList = new();
        ListSV containerList = new();
        PaneSV previewPane = new()
        {
            X = 2,
            Y = 1,
            SubViews = []
        };
        LabelSV topLabel = new()
        {
            X = 0,
            Y = 0,
            Properties = [],
            TextFields = []
        };
        LabelSV bottomLabel = new()
        {
            X = 0,
            Y = 2,
            Properties = [],
            TextFields = []
        };

        RootSV rootSV = new()
        {
            RootPane = new()
            {
                SubViews =
                [
                    topLabel,
                    new BorderSV()
                    {
                        X = 0,
                        Y = 1,
                        FillStyleX = SVFillStyle.Percent,
                        PercentX = 0.125f,
                        SVBorderStyle = SVBorderStyle.Right,
                        SubPane = new()
                        {
                            SubViews =
                            [
                                containerList
                            ]
                        }
                    },
                    new BorderSV()
                    {
                        X = 1,
                        Y = 1,
                        FillStyleX = SVFillStyle.Percent,
                        PercentX = 0.425f,
                        SVBorderStyle = SVBorderStyle.Right,
                        SubPane = new()
                        {
                            SubViews =
                            [
                                currentList
                            ]
                        }
                    },
                    previewPane,
                    bottomLabel,

                ]
            }
        };

        await setupTask;

        await rootSV.Arrange();
        await rootSV.Draw();

        var currentListTask = Task.Run(async () =>
        {
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
            bottomLabel.TextFields.Add(new()
            {
                Text = "Bottom Label"
            });
            bottomLabel.Properties.Add(TextProperty.Bold);

            await bottomLabel.Arrange();
            await bottomLabel.Draw();
        });

        await Task.WhenAll(currentListTask, topLabelTask, bottomLabelTask);

        //var renderTask = Task.Run(RenderLoop);

        //await Task.WhenAll(inputTask, renderTask);
        await inputTask;
        //await Task.Delay(-1);

        Console.Clear();
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

        //Stopwatch sw = new();
        //sw.Restart();
        await TUIRenderer.ExecuteRenderAction(_appState.RootView, RenderAction.Arrange);
        //sw.Stop();
        //await Console.Error.WriteLineAsync($"TUIRenderer {sw.ElapsedMilliseconds}ms");

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