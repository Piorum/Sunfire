using Sunfire.Tui.Models;
using Sunfire.Tui.Interfaces;

namespace Sunfire.Image.Views;

public class ImageSV : ISunfireView
{
    public int OriginX { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
    public int OriginY { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
    public int SizeX { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
    public int SizeY { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

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
