using Sunfire.Enums;
using Sunfire.Factories;
using Sunfire.Views;

namespace Sunfire;

public class AppState
{
    public View RootView { private set; get; } = new() { X = 0, Y = 0, FillStyleWidth = FillStyle.Max, FillStyleHeight = FillStyle.Max };

    private readonly ViewLabel TopLabel;
    private readonly ViewLabel BottomLabel;

    private readonly View ContainerPane;
    private readonly ViewList CurrentPane;
    private readonly View PreviewPane;

    public AppState()
    {
        RootView.Add(ViewFactory.GetTopLabel());
        RootView.Add(ViewFactory.GetBottomLabel());
        RootView.Add(ViewFactory.GetContainerPane());
        RootView.Add(ViewFactory.GetCurrentPane());
        RootView.Add(ViewFactory.GetPreviewPane());

        TopLabel = RootView.SubViews.OfType<ViewLabel>().Where(l => l.Tag == "Top Label").First();
        BottomLabel = RootView.SubViews.OfType<ViewLabel>().Where(l => l.Tag == "Bottom Label").First();
        ContainerPane = RootView.SubViews.Where(sv => sv.Tag == "Container Pane").First();
        CurrentPane = RootView.SubViews.OfType<ViewList>().Where(sv => sv.Tag == "Current Pane").First();
        PreviewPane = RootView.SubViews.Where(sv => sv.Tag == "Preview Pane").First();

        Task.Run(async () =>
        {
            List<ViewLabel> newLabels = [];
            for (int i = 0; i < 100; i++)
            {
                var newLabel = new ViewLabel()
                {
                    X = 0,
                    Y = 0,
                    FillStyleWidth = FillStyle.Max,
                    FillStyleHeight = FillStyle.Min
                };
                newLabel.TextFields.Add(new()
                {
                    Text = $"{i}"
                });
                newLabels.Add(newLabel);
            }
            CurrentPane.Add(newLabels);
            await CurrentPane.Arrange(CurrentPane.SizeX, CurrentPane.SizeY);
        });
    }

    public async Task UpdateTopLabel(string text, bool? highlight = null, bool? bold = null)
    {
        var textField = TopLabel.TextFields.First();
        textField.Text = text;

        if (highlight is not null)
            TopLabel.Highlighted = (bool)highlight;
        if (bold is not null)
            TopLabel.Bold = (bool)bold;

        await TopLabel.Draw();
    }

    public async Task MoveDown()
    {
        await CurrentPane.MoveDown();
    }
    public async Task MoveUp()
    {
        await CurrentPane.MoveUp();
    }

}
