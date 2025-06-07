using System.Diagnostics;
using Sunfire.Registries;
using SunfireFramework.Enums;
using SunfireFramework.Terminal;
using SunfireFramework.Views.TextBoxes;

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
                var newLabel = new LabelSVSlim()
                {
                    Text = $"{i}"
                };
                await currentList.AddLabel(newLabel);
            }

            currentList.selectedIndex = 5;

            await currentList.Arrange();
            await currentList.Draw();
        });

        var topLabelTask = Task.Run(async () =>
        {
            var topLeftLabel = SVRegistry.GetTopLeftLabel();
            topLeftLabel.Text = "Top Label";
            topLeftLabel.Properties |= TextProperty.Bold;

            await topLeftLabel.Arrange();
            await topLeftLabel.Draw();
        });

        var bottomLabelTask = Task.Run(async () =>
        {
            var bottomLabel = SVRegistry.GetBottomLabel();
            bottomLabel.Text = "Bottom Label";
            bottomLabel.Properties |= TextProperty.Bold;

            await bottomLabel.Arrange();
            await bottomLabel.Draw();
        });

        await Task.WhenAll(currentListTask, topLabelTask, bottomLabelTask);
    }
}
