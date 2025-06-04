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
        Stopwatch sw = new();
        sw.Restart();
        var inputTask = Task.Run(InputLoop);

        Console.CursorVisible = false;
        Console.Clear();

        ListSV testList = new();

        RootSV rootSV = new()
        {
            RootPane = new()
            {
                SubViews =
                [
                    new SVLabel()
                    {
                        X = 0,
                        Y = 0,
                        Properties = [ TextProperty.Bold ],
                        TextFields =
                        [
                            new()
                            {
                                Text = "Hello World!"
                            }
                        ]
                    },
                    new BorderSV()
                    {
                        X = 0,
                        Y = 1,
                        FillStyleX = SVFillStyle.Percent,
                        PercentX = 0.125f,
                        SVBorderStyle = SVBorderStyle.Right,
                        SubPane = new()
                        {
                            SubViews = []
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
                                testList
                            ]
                        }
                    },
                    new PaneSV()
                    {
                        X = 2,
                        Y = 1,
                        SubViews = []
                    },
                    new SVLabel()
                    {
                        X = 0,
                        Y = 2,
                        Properties = [ TextProperty.Bold ],
                        TextFields =
                        [
                            new()
                            {
                                Text = "Bottom Label"
                            }
                        ]
                    },

                ]
            }
        };

        await rootSV.Arrange();
        await rootSV.Draw();
        sw.Stop();
        //grey values because this isn't being written though framework consolewriter
        await Console.Error.WriteLineAsync($"{sw.Elapsed.Seconds}s {sw.Elapsed.Milliseconds}ms {sw.Elapsed.Microseconds}μs");


        for (int i = 0; i < 10; i++)
        {
            await testList.AddLabel(new()
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

        await testList.Arrange();
        await testList.Draw();

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