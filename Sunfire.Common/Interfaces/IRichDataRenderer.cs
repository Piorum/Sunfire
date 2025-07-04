namespace Sunfire.Common.Interfaces;

public interface IRichDataRenderer
{
    Task Render(IRichData data);
    Task Clear(IRichData data);
}
