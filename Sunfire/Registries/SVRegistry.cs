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
    private static ListSV? containerList;
    private static PaneSV? previewPane;
    private static LabelSV? topLeftLabel;
    private static LabelSV? topRightLabel;
    private static LabelSV? bottomLabel;

    [ModuleInitializer]
    public static void Init()
    {
        currentList = new();
        containerList = new();

        previewPane = new()
        {
            SubViews = []
        };

        topLeftLabel = new()
        {
            X = 0,
            Y = 0,
        };
        topRightLabel = new()
        {
            X = 1,
            Y = 0,
            FillStyleX = FillStyle.Static,
            Text = "Test"
        };
        topRightLabel.StaticX = topRightLabel.Text.Length;

        bottomLabel = new()
        {
            X = 0,
            Y = 2,
        };

        rootSV = new()
        {
            RootView = new PaneSV()
            {
                SubViews =
                [
                    topLeftLabel,
                    topRightLabel,
                    new BorderSV()
                    {
                        BorderSides = Direction.Top | Direction.Bottom | Direction.Left,
                        SubPane = new()
                        {
                            X = 0,
                            Y = 1,
                            FillStyleX = FillStyle.Percent,
                            PercentX = 0.125f,
                            SubViews =
                            [
                                containerList
                            ]
                        }
                    },
                    new BorderSV()
                    {
                        BorderSides = Direction.Top | Direction.Bottom | Direction.Left,
                        SubPane = new()
                        {
                            X = 1,
                            Y = 1,
                            FillStyleX = FillStyle.Percent,
                            PercentX = 0.425f,
                            SubViews =
                            [
                                currentList
                            ]
                        }
                    },
                    new BorderSV()
                    {
                        BorderSides = Direction.Top | Direction.Bottom | Direction.Left | Direction.Right,
                        SubPane = new()
                        {
                            X = 2,
                            Y = 1,
                            SubViews =
                            [
                                previewPane
                            ]
                        }
                    },
                    bottomLabel,

                ]
            }
        };
    }

    public static RootSV GetRootSV() =>
        rootSV!;

    public static ListSV GetCurrentList() =>
        currentList!;
    public static ListSV GetContainerList() =>
        containerList!;
    public static PaneSV GetPreviewPane() =>
        previewPane!;
    public static LabelSV GetTopLeftLabel() =>
        topLeftLabel!;
    public static LabelSV GetTopRightLabel() =>
        topRightLabel!;
    public static LabelSV GetBottomLabel() =>
        bottomLabel!;
}
