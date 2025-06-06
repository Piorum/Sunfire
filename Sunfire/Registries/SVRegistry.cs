using SunfireFramework.Views;
using SunfireFramework.TextBoxes;
using SunfireFramework.Enums;
using System.Runtime.CompilerServices;

namespace Sunfire.Registries;

public static class SVRegistry
{
    private static RootSV? rootSV;

    private static ListSV? currentList;
    private static ListSV? containerList;
    private static PaneSV? previewPane;
    private static LabelSV? topLabel;
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

        topLabel = new()
        {
            X = 0,
            Y = 0,
            Properties = [],
            TextFields = []
        };

        bottomLabel = new()
        {
            X = 0,
            Y = 2,
            Properties = [],
            TextFields = []
        };

        rootSV = new()
        {
            RootPane = new()
            {
                SubViews =
                [
                    topLabel,
                    new BorderSV()
                    {
                        X = 0,
                        Y = 1,
                        FillStyleX = SVFillStyle.Percent,
                        PercentX = 0.125f,
                        SVBorderStyle = SVBorderStyle.Right,
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
                        SVBorderStyle = SVBorderStyle.Right,
                        SubPane = new()
                        {
                            SubViews =
                            [
                                currentList
                            ]
                        }
                    },
                    previewPane,
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
    public static LabelSV GetTopLabel() =>
        topLabel!;
    public static LabelSV GetBottomLabel() =>
        bottomLabel!;
}
