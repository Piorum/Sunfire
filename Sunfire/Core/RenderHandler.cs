using System.Runtime.InteropServices;
using Sunfire.Registries;
using SunfireFramework.Enums;

namespace Sunfire.Core;

[System.Runtime.Versioning.SupportedOSPlatform("linux")]
public static class RenderHandler
{
    public static readonly ManualResetEventSlim _renderSignal = new();

#pragma warning disable IDE0052
    //Static store for sigwinch so it doesn't get garbage collected
    private static PosixSignalRegistration? sigwinchRegistration;
#pragma warning restore IDE0052

    public static async Task RenderLoop(CancellationToken token)
    {
        await RegisterResizeEvent();

        //Initial Draw
        var rootSv = SVRegistry.GetRootSV();
        await rootSv.Arrange();
        await rootSv.Draw();
        await PopulateFields();

        while (!token.IsCancellationRequested)
        {
            try
            {
                _renderSignal.Wait(token);
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
                    var rootSV = SVRegistry.GetRootSV();

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
            var currentList = SVRegistry.GetCurrentList();
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
            var topLabel = SVRegistry.GetTopLabel();
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
            var bottomLabel = SVRegistry.GetBottomLabel();
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
