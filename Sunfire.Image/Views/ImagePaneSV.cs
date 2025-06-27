using Sunfire.Tui.Enums;
using Sunfire.Tui.Models;
using Sunfire.Tui.Interfaces;

namespace Sunfire.Image.Views;

public class ImagePaneSV : IRelativeSunfireView
{
    public int X => throw new NotImplementedException();

    public int Y => throw new NotImplementedException();

    public int Z => throw new NotImplementedException();

    public FillStyle FillStyleX => throw new NotImplementedException();

    public FillStyle FillStyleY => throw new NotImplementedException();

    public int StaticX => throw new NotImplementedException();

    public int StaticY => throw new NotImplementedException();

    public float PercentX => throw new NotImplementedException();

    public float PercentY => throw new NotImplementedException();

    public int OriginX { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
    public int OriginY { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
    public int SizeX { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
    public int SizeY { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

    public int MinX => throw new NotImplementedException();

    public int MinY => throw new NotImplementedException();

    public Task<bool> Arrange()
    {
        throw new NotImplementedException();
    }

    public Task Draw(SVContext context)
    {
        throw new NotImplementedException();
    }

    public Task Invalidate()
    {
        throw new NotImplementedException();
    }
}
