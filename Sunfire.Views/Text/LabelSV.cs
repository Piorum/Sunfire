using Sunfire.Tui.Enums;
using Sunfire.Tui.Interfaces;

namespace Sunfire.Views.Text;

public class LabelSV : LabelSVSlim, IRelativeSunfireView
{
    public int X { get; set; }
    public int Y { get; set; }
    public int Z { get; set; }

    public FillStyle FillStyleX { set; get; } = FillStyle.Max;
    public FillStyle FillStyleY { set; get; } = FillStyle.Min;
    public int StaticX { set; get; } = 1; //1 = 1 Cell
    public int StaticY { set; get; } = 1; //1 = 1 Cell
    public float PercentX { set; get; } = 1.0f; //1.0f == 100%
    public float PercentY { set; get; } = 1.0f; //1.0f == 100%

    public int MinX { get; } = 0;
    public int MinY { get; } = 1;

}
