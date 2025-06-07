using Sunfire.Registries;
using SunfireFramework.Enums;

namespace Sunfire.Core;

public static class RenderHandler
{
    public static readonly ManualResetEventSlim _renderSignal = new();

    public static async Task RenderLoop(CancellationToken token)
    {
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
