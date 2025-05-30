using Sunfire.Views;
using Sunfire.Enums;
using System.Runtime.InteropServices;

namespace Sunfire;

[System.Runtime.Versioning.SupportedOSPlatform("linux")]
internal class Program
{
    public static readonly CancellationTokenSource _cts = new();
    public static readonly ManualResetEventSlim _renderSignal = new();

    public static readonly View RootView = new() { X = 0, Y = 0, FillStyleWidth = FillStyle.Max, FillStyleHeight = FillStyle.Max };

    public static async Task Main()
    {
        Console.CursorVisible = false;
        Console.Clear();

        await AddTestViews();

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
        }
        return Task.CompletedTask;
    }

    private static async Task RenderLoop()
    {
        await RegisterResizeEvent();
        await Renderer.ExecuteRenderAction(RootView, RenderAction.Arrange);

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

            await Renderer.ExecuteRenderTasks(RootView);

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
                await Renderer.ExecuteRenderAction(RootView, RenderAction.Arrange);
            });
        });
        return Task.CompletedTask;
    }

    private static async Task AddTestViews()
    {
        var testView1 = new View()
        {
            X = 0,
            Y = 0,
            FillStyleWidth = FillStyle.Max,
            FillStyleHeight = FillStyle.Min,
        };
        var testView2 = new View()
        {
            X = 0,
            Y = 1,
            FillStyleWidth = FillStyle.Percent,
            WidthPercent = 0.125f,
            FillStyleHeight = FillStyle.Max,
            BorderStyle = BorderStyle.Right
        };
        var testView3 = new View()
        {
            X = 1,
            Y = 1,
            FillStyleWidth = FillStyle.Percent,
            WidthPercent = 0.425f,
            FillStyleHeight = FillStyle.Max,
            BorderStyle = BorderStyle.Right,
        };
        var testView4 = new View()
        {
            X = 2,
            Y = 1,
            FillStyleWidth = FillStyle.Max,
            FillStyleHeight = FillStyle.Max,
        };
        var testView5 = new View()
        {
            X = 0,
            Y = 2,
            FillStyleWidth = FillStyle.Max,
            FillStyleHeight = FillStyle.Min,
        };

        var testLabel0 = new Label()
        {
            X = 0,
            Y = 0,
            FillStyleWidth = FillStyle.Max,
            FillStyleHeight = FillStyle.Min,
            Bold = false,
            Highlighted = false
        };
        testLabel0.TextFields.Add(new()
        {
            Text = "Top Bar"
        });
        await testView1.AddAsync(testLabel0);

        await RootView.AddAsync(testView1);
        await RootView.AddAsync(testView2);

        var testLabel1 = new Label()
        {
            X = 0,
            Y = 0,
            FillStyleWidth = FillStyle.Max,
            FillStyleHeight = FillStyle.Min,
            Bold = true,
            Highlighted = true
        };
        testLabel1.TextFields.Add(new()
        {
            Text = "Hellooooooooooooooooooooooooooooo"
        });
        var testLabel2 = new Label()
        {
            X = 0,
            Y = 1,
            FillStyleWidth = FillStyle.Max,
            FillStyleHeight = FillStyle.Min,
            Bold = true,
        };
        testLabel2.TextFields.Add(new()
        {
            Text = "Worldooooooooooooooooooooooooooooo"
        });
        var testLabel3 = new Label()
        {
            X = 0,
            Y = 2,
            FillStyleWidth = FillStyle.Max,
            FillStyleHeight = FillStyle.Min
        };
        testLabel3.TextFields.Add(new()
        {
            Text = "Testooooooooooooooooooooooooooooooo"
        });
        await testView3.AddAsync(testLabel1);
        await testView3.AddAsync(testLabel2);
        await testView3.AddAsync(testLabel3);

        await RootView.AddAsync(testView3);
        await RootView.AddAsync(testView4);

        var testLabel5 = new Label()
        {
            X = 0,
            Y = 0,
            FillStyleWidth = FillStyle.Max,
            FillStyleHeight = FillStyle.Min,
            Bold = false,
            Highlighted = false
        };
        testLabel5.TextFields.Add(new()
        {
            Text = "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA"
        });
        testLabel5.TextFields.Add(new()
        {
            Z = 1,
            Text = "BBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBB",
            AlignSide = AlignSide.Right
        });
        await testView5.AddAsync(testLabel5);

        await RootView.AddAsync(testView5);
    }
}