using Sunfire.Tui;
using Sunfire.Tui.Enums;
using Sunfire.Views;
using Sunfire.Views.Text;
using System.Runtime.CompilerServices;

namespace Sunfire.Registries;

public static class SVRegistry
{
    private static RootSV? rootSV;

    private static EntriesListView? containerList;
    private static PaneSV? containerPane;
    private static BorderSV? containerBorder;

    private static ListSV? currentList;
    private static PaneSV? currentPane;
    private static BorderSV? currentBorder;

    private static PaneSV? previewPane;
    private static BorderSV? previewBorder;

    private static LabelSV? bottomRightLabel;
    private static BorderSV? bottomRightBorder;
    private static LabelSV? bottomLeftLabel;
    private static BorderSV? bottomLeftBorder;

    [ModuleInitializer]
    public static void Init()
    {
        bottomLeftLabel = new()
        {
            X = 0,
            Y = 1,
        };
        bottomLeftBorder = new()
        {
            SubView = bottomLeftLabel
        };
        bottomRightLabel = new()
        {
            X = 1,
            Y = 1,
            FillStyleX = FillStyle.Static,
            Segments = [new() { Text = $"{Environment.UserName}@{Environment.UserDomainName}" }]
        };
        bottomRightLabel.StaticX = bottomRightLabel.Segments.Sum(e => e.Text.Length);
        bottomRightBorder = new()
        {
            SubView = bottomRightLabel
        };

        previewPane = new()
        {
            X = 2,
            Y = 0,
            SubViews = []
        };
        previewBorder = new()
        {
            SubView = previewPane
        };

        currentList = new();
        currentPane = new()
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
        currentBorder = new()
        {
            SubView = currentPane
        };

        containerList = new();
        containerPane = new()
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
        containerBorder = new()
        {
            SubView = containerPane
        };

        rootSV = new()
        {
            RootView = new PaneSV()
            {
                SubViews =
                [
                    containerBorder,
                    currentBorder,
                    previewBorder,
                    bottomLeftBorder,
                    bottomRightBorder
                ]
            }
        };
    }

    public static RootSV RootSV =>
        rootSV!;

    public static EntriesListView ContainerList =>
        containerList!;
    public static PaneSV ContainerPane =>
        containerPane!;
    public static BorderSV ContainerBorder =>
        containerBorder!;

    public static ListSV CurrentList =>
        currentList!;
    public static PaneSV CurrentPane =>
        currentPane!;
    public static BorderSV CurrentBorder =>
        currentBorder!;

    public static PaneSV PreviewPane =>
        previewPane!;
    public static BorderSV PreviewBorder =>
        previewBorder!;

    public static LabelSV BottomRightLabel =>
        bottomRightLabel!;
    public static BorderSV BottomRightBorder =>
        bottomRightBorder!;
    public static LabelSV BottomLeftLabel =>
        bottomLeftLabel!;
    public static BorderSV BottomLeftBorder =>
        bottomLeftBorder!;
}
