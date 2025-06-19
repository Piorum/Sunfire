using SunfireFramework.Enums;
using SunfireFramework.Rendering;

namespace SunfireFramework.Views;

public interface IRelativeSunfireView : ISunfireView
{
    int X { get; }
    int Y { get; }
    int Z { get; }

    SVFillStyle FillStyleX { get; }
    SVFillStyle FillStyleY { get; }
    int StaticX { get; } //1 = 1 Cell
    int StaticY { get; } //1 = 1 Cell
    float PercentX { get; } //1.0f == 100%
    float PercentY { get; } //1.0f == 100%
}

public interface ISunfireView
{
    int OriginX { set; get; } // Top Left
    int OriginY { set; get; } // Top Left
    int SizeX { set; get; } // Width
    int SizeY { set; get; } // Height

    Task<bool> Arrange(); //returns true if work done (needs redraw)

    Task Draw(SVContext context);

    Task Invalidate();
}
