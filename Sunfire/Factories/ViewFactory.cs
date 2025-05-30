using Sunfire.Enums;
using Sunfire.Views;

namespace Sunfire.Factories;

public static class ViewFactory
{
    public static View GetTopLabel()
    {
        var view = new View()
        {
            Tag = "Top Pane",
            X = 0,
            Y = 0,
            FillStyleWidth = FillStyle.Max,
            FillStyleHeight = FillStyle.Min,
        };

        var topLabel = new Label()
        {
            Tag = "Top Label",
            X = 0,
            Y = 0,
            FillStyleWidth = FillStyle.Max,
            FillStyleHeight = FillStyle.Min
        };
        topLabel.TextFields.Add(new()
        {
            Text = "Top Bar"
        });
        view.Add(topLabel);

        return view;
    }
    public static View GetBottomLabel()
    {
        var view = new View()
        {
            Tag = "Bottom Pane",
            X = 0,
            Y = 2,
            FillStyleWidth = FillStyle.Max,
            FillStyleHeight = FillStyle.Min,
        };

        var bottomLabel = new Label()
        {
            Tag = "Bottom Label",
            X = 0,
            Y = 0,
            FillStyleWidth = FillStyle.Max,
            FillStyleHeight = FillStyle.Min,
        };
        bottomLabel.TextFields.Add(new()
        {
            Text = "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA"
        });
        bottomLabel.TextFields.Add(new()
        {
            Z = 1,
            Text = "BBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBB",
            AlignSide = AlignSide.Right
        });
        view.Add(bottomLabel);

        return view;
    }
    public static View GetContainerPane()
    {
        var view = new View()
        {
            Tag = "Container Pane",
            X = 0,
            Y = 1,
            FillStyleWidth = FillStyle.Percent,
            WidthPercent = 0.125f,
            FillStyleHeight = FillStyle.Max,
            BorderStyle = BorderStyle.Right
        };
        return view;
    }
    public static View GetCurrentPane()
    {
        var view = new View()
        {
            Tag = "Current Pane",
            X = 1,
            Y = 1,
            FillStyleWidth = FillStyle.Percent,
            WidthPercent = 0.425f,
            FillStyleHeight = FillStyle.Max,
            BorderStyle = BorderStyle.Right,
        };
        return view;
    }
    public static View GetPreviewPane()
    {
        var view = new View()
        {
            Tag = "Preview Pane",
            X = 2,
            Y = 1,
            FillStyleWidth = FillStyle.Max,
            FillStyleHeight = FillStyle.Max,
        };
        return view;
    }
        
}
