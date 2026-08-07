using Moonfire.Tui;
using Sunfire.Views;
using Sunfire.Views.Text;
using System.Runtime.CompilerServices;
using Moonfire.Rendering.Enums;
using Wrath.Views;


namespace Sunfire.Registries;

public static class SVRegistry
{
    private static EntriesListView? containerList;
    private static Pane? containerPane;
    private static Border? containerBorder;

    private static EntriesListView? currentList;
    private static Pane? currentPane;
    private static Border? currentBorder;

    private static PreviewView? previewPane;
    private static Border? previewBorder;

    private static LabelSV? bottomRightLabel;
    private static Border? bottomRightBorder;
    private static SelectionInfoView? selectionInfoView;

    private static InfosView? infosView;

    private static Pane? rootPane;

    [ModuleInitializer]
    public static void Init()
    {
        var selectionInfoLabel = new LabelSV()
        {
            X = 0,
            Y = 2,
        };
        selectionInfoView = new(selectionInfoLabel)
        {
            SubView = selectionInfoLabel
        };

        bottomRightLabel = new()
        {
            X = 1,
            Y = 2,
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

        infosView = new()
        {
            X = 0,
            Y = 1,
        };

        rootPane = new()
        {
            SubViews =
                [
                    containerBorder,
                    currentBorder,
                    previewBorder,
                    infosView,
                    selectionInfoView,
                    bottomRightBorder,
                ]
        };
    
    }

    public static EntriesListView ContainerList =>
        containerList!;
    public static Pane ContainerPane =>
        containerPane!;
    public static Border ContainerBorder =>
        containerBorder!;

    public static EntriesListView CurrentList =>
        currentList!;
    public static Pane CurrentPane =>
        currentPane!;
    public static Border CurrentBorder =>
        currentBorder!;

    public static PreviewView PreviewView =>
        previewPane!;
    public static Border PreviewBorder =>
        previewBorder!;

    public static LabelSV BottomRightLabel =>
        bottomRightLabel!;
    public static Border BottomRightBorder =>
        bottomRightBorder!;
    public static SelectionInfoView SelectionInfoView =>
        selectionInfoView!;

    public static InfosView InfosView =>
        infosView!;

    public static Pane RootPane =>
        rootPane!;
}
