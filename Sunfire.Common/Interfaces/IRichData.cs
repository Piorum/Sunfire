using System.Drawing;

namespace Sunfire.Common.Interfaces;

public interface IRichData
{
    Rectangle Bounds { set; get; }

    Guid InstanceId { get; }

    Task<Size> Measure();
    Task Prepare();
}
