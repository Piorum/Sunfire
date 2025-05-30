using Sunfire.Enums;
using Sunfire.Factories;
using Sunfire.Views;

namespace Sunfire;

public class AppState
{
    public View RootView { private set; get; } = new() { X = 0, Y = 0, FillStyleWidth = FillStyle.Max, FillStyleHeight = FillStyle.Max };

    private readonly Label TopLabel;
    private readonly Label BottomLabel;

    private readonly View ContainerPane;
    private readonly View CurrentPane;
    private readonly View PreviewPane;

    public AppState()
    {
        RootView.Add(ViewFactory.GetTopLabel());
        RootView.Add(ViewFactory.GetBottomLabel());
        RootView.Add(ViewFactory.GetContainerPane());
        RootView.Add(ViewFactory.GetCurrentPane());
        RootView.Add(ViewFactory.GetPreviewPane());

        TopLabel = RootView.SubViews.Where(sv => sv.Tag == "Top Pane").First().SubViews.OfType<Label>().Where(l => l.Tag == "Top Label").First();
        BottomLabel = RootView.SubViews.Where(sv => sv.Tag == "Bottom Pane").First().SubViews.OfType<Label>().Where(l => l.Tag == "Bottom Label").First();
        ContainerPane = RootView.SubViews.Where(sv => sv.Tag == "Container Pane").First();
        CurrentPane = RootView.SubViews.Where(sv => sv.Tag == "Current Pane").First();
        PreviewPane = RootView.SubViews.Where(sv => sv.Tag == "Preview Pane").First();
    }

    public Task UpdateTopLabel(string text, bool? highlight = null, bool? bold = null)
    {
        var textField = TopLabel.TextFields.First();
        textField.Text = text;

        if (highlight is not null)
            TopLabel.Highlighted = (bool)highlight;
        if (bold is not null)
            TopLabel.Bold = (bool)bold;

        TopLabel.Draw();

        return Task.CompletedTask;
    }

}
