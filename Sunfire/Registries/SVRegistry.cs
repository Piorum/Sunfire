using Sunfire.Tui;
using Sunfire.Tui.Enums;
using Sunfire.Views;
using Sunfire.Views.Text;
using System.Runtime.CompilerServices;

namespace Sunfire.Registries;

public static class SVRegistry
{
    private static RootSV? rootSV;

    private static ListSV? currentList;
    private static PaneSV? currentPane;
    private static ListSV? containerList;
    private static PaneSV? containerPane;
    private static PaneSV? previewPane;
    private static LabelSV? topLeftLabel;
    private static LabelSV? topRightLabel;
    private static LabelSV? bottomLabel;

    [ModuleInitializer]
    public static void Init()
    {
        currentList = new();
        containerList = new();

        topLeftLabel = new()
        {
            X = 0,
            Y = 0,
        };
        topRightLabel = new()
        {
            X = 1,
            Y = 1,
            FillStyleX = FillStyle.Static,
            Text = $"{Environment.UserName}@{Environment.UserDomainName}"
        };
        topRightLabel.StaticX = topRightLabel.Text.Length;

        bottomLabel = new()
        {
            X = 0,
            Y = 1,
        };

        containerPane = new PaneSV()
        {
            X = 0,
            Y = 0,
            FillStyleX = FillStyle.Percent,
            PercentX = 0.125f,
            SubViews =
            [
                containerList
            ]
        };
        currentPane = new PaneSV()
        {
            X = 1,
            Y = 0,
            FillStyleX = FillStyle.Percent,
            PercentX = 0.425f,
            SubViews =
            [
                currentList
            ]
        };
        previewPane = new()
        {
            X = 2,
            Y = 0,
            SubViews = []
        };

        rootSV = new()
        {
            RootView = new PaneSV()
            {
                SubViews =
                [
                    new BorderSV(){
                        SubView = containerPane
                    },
                    new BorderSV(){
                        TitleLabel = new()
                        {
                            Text = "~/.config/hypr/hyprext/variables.conf"
                        },
                        SubView = currentPane
                    },
                    new BorderSV(){
                        SubView = previewPane
                    },
                    new BorderSV(){
                        TitleLabel = new()
                        {
                            Text = "Bottom Border"
                        },
                        SubView = bottomLabel
                    },
                    new BorderSV(){
                        SubView = topRightLabel
                    },
                ]
            }
        };
    }

    public static RootSV GetRootSV() =>
        rootSV!;

    public static ListSV GetCurrentList() =>
        currentList!;
    public static PaneSV GetCurrentPane() =>
        currentPane!;
    public static ListSV GetContainerList() =>
        containerList!;
    public static PaneSV GetContainerPane() =>
        containerPane!;
    public static PaneSV GetPreviewPane() =>
        previewPane!;
    public static LabelSV GetTopLeftLabel() =>
        topLeftLabel!;
    public static LabelSV GetTopRightLabel() =>
        topRightLabel!;
    public static LabelSV GetBottomLabel() =>
        bottomLabel!;
}
