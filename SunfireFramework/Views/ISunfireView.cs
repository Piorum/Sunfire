using SunfireFramework.Enums;
using SunfireFramework.Rendering;

namespace SunfireFramework.Views;

public interface IRelativeSunfireView : ISunfireView
{
    int X { set; get; }
    int Y { set; get; }
    int Z { set; get; }

    SVFillStyle FillStyleX { set; get; }
    SVFillStyle FillStyleY { set; get; }
    int StaticX { set; get; } //1 = 1 Cell
    int StaticY { set; get; } //1 = 1 Cell
    float PercentX { set; get; } //1.0f == 100%
    float PercentY { set; get; } //1.0f == 100%
}

public interface ISunfireView
{
    int OriginX { set; get; } // Top Left
    int OriginY { set; get; } // Top Left
    int SizeX { set; get; } // Width
    int SizeY { set; get; } // Height

    Task Arrange();

    Task Draw(SVContext context);
}
