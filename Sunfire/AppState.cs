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
        TopLabel = ViewFactory.GetTopLabel();
        BottomLabel = ViewFactory.GetBottomLabel();

        ContainerPane = ViewFactory.GetContainerPane();
        CurrentPane = ViewFactory.GetCurrentPane();
        PreviewPane = ViewFactory.GetPreviewPane();

        RootView.Add(TopLabel);
        RootView.Add(BottomLabel);
        RootView.Add(ContainerPane);
        RootView.Add(CurrentPane);
        RootView.Add(PreviewPane);

        Task.Run(async () =>
        {
            List<ViewLabel> newLabels = [];
            for (int i = 0; i < 1000000; i++)
            {
                var newLabel = new ViewLabel()
                {
                    X = 0,
                    Y = 0,
                    FillStyleWidth = FillStyle.Max,
                    FillStyleHeight = FillStyle.Min,
                    Bold = true
                };
                newLabel.TextFields.Add(new()
                {
                    Text = $"{i}"
                });
                newLabels.Add(newLabel);
            }
            CurrentPane.Add(newLabels);
            CurrentPane.LoadingSignal = false;
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

    public async Task MoveDown() =>
        await CurrentPane.MoveDown();
    public async Task MoveUp() =>
        await CurrentPane.MoveUp();
    public async Task MoveTop() =>
        await CurrentPane.ToTop();
    public async Task MoveBottom() =>
        await CurrentPane.ToBottom();

    public async Task Select() =>
        await CurrentPane.SelectCurrent();

    public async Task Add(List<TextFields> text)
    {
        ViewLabel vl = new()
        {
            X = 0,
            Y = 0,
            FillStyleHeight = FillStyle.Min,
            FillStyleWidth = FillStyle.Max,
        };
        vl.TextFields.AddRange(text);
        CurrentPane.Add(vl);
        //Slowest part of adding an item
        await CurrentPane.Arrange(CurrentPane.SizeX, CurrentPane.SizeY);
    }

    public async Task Delete() =>
        await CurrentPane.Remove();

}
