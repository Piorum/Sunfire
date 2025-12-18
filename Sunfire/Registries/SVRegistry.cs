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

    private static EntriesListView? currentList;
    private static PaneSV? currentPane;
    private static BorderSV? currentBorder;

    private static PreviewView? previewPane;
    private static BorderSV? previewBorder;

    private static LabelSV? bottomRightLabel;
    private static BorderSV? bottomRightBorder;
    private static SelectionInfoView? selectionInfoView;

    private static PaneSV? rootPane;

    [ModuleInitializer]
    public static void Init()
    {
        var selectionInfoLabel = new LabelSV()
        {
            X = 0,
            Y = 1,
        };
        selectionInfoView = new(selectionInfoLabel)
        {
            SubView = selectionInfoLabel
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

        rootPane = new()
        {
            SubViews =
                [
                    containerBorder,
                    currentBorder,
                    previewBorder,
                    selectionInfoView,
                    bottomRightBorder
                ]
        };
    
        rootSV = new()
        {
            RootView = rootPane
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

    public static EntriesListView CurrentList =>
        currentList!;
    public static PaneSV CurrentPane =>
        currentPane!;
    public static BorderSV CurrentBorder =>
        currentBorder!;

    public static PreviewView PreviewView =>
        previewPane!;
    public static BorderSV PreviewBorder =>
        previewBorder!;

    public static LabelSV BottomRightLabel =>
        bottomRightLabel!;
    public static BorderSV BottomRightBorder =>
        bottomRightBorder!;
    public static SelectionInfoView SelectionInfoView =>
        selectionInfoView!;

    public static PaneSV RootPane =>
        rootPane!;
}
