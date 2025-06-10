using SunfireFramework.Views;
using SunfireFramework.Views.TextBoxes;
using SunfireFramework.Enums;
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
            FillStyleX = SVFillStyle.Static,
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
            RootPane = new()
            {
                SubViews =
                [
                    topLeftLabel,
                    topRightLabel,
                    new BorderSV()
                    {
                        X = 0,
                        Y = 1,
                        FillStyleX = SVFillStyle.Percent,
                        PercentX = 0.125f,
                        BorderStyle = SVBorderStyle.Top | SVBorderStyle.Bottom | SVBorderStyle.Left,
                        SubPane = new()
                        {
                            SubViews =
                            [
                                containerList
                            ]
                        }
                    },
                    new BorderSV()
                    {
                        X = 1,
                        Y = 1,
                        FillStyleX = SVFillStyle.Percent,
                        PercentX = 0.425f,
                        BorderStyle = SVBorderStyle.Top | SVBorderStyle.Bottom | SVBorderStyle.Left,
                        SubPane = new()
                        {
                            SubViews =
                            [
                                currentList
                            ]
                        }
                    },
                    new BorderSV()
                    {
                        X = 2,
                        Y = 1,
                        BorderStyle = SVBorderStyle.Top | SVBorderStyle.Bottom | SVBorderStyle.Left | SVBorderStyle.Right,
                        SubPane = new()
                        {
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
