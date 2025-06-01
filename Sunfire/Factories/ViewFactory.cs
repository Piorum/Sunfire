using Sunfire.Enums;
using Sunfire.Views;

namespace Sunfire.Factories;

public static class ViewFactory
{
    public static View GetTopLabel()
    {
        var topLabel = new ViewLabel()
        {
            Tag = "Top Label",
            X = 0,
            Y = 0,
            FillStyleWidth = FillStyle.Max,
            FillStyleHeight = FillStyle.Min,
            Bold = true
        };
        topLabel.TextFields.Add(new()
        {
            Text = "Top Bar"
        });

        return topLabel;
    }
    public static View GetBottomLabel()
    {
        var bottomLabel = new ViewLabel()
        {
            Tag = "Bottom Label",
            X = 0,
            Y = 2,
            FillStyleWidth = FillStyle.Max,
            FillStyleHeight = FillStyle.Min,
            Bold = true
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

        return bottomLabel;
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
    public static ViewList GetCurrentPane()
    {
        var view = new ViewList()
        {
            Tag = "Current Pane",
            X = 1,
            Y = 1,
            FillStyleWidth = FillStyle.Percent,
            WidthPercent = 0.425f,
            FillStyleHeight = FillStyle.Max,
            BorderStyle = BorderStyle.Right,
            LoadingSignal = true
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
