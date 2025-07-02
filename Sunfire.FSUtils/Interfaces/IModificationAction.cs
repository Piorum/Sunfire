
using Sunfire.FSUtils.Models;

namespace Sunfire.FSUtils.Interfaces
{
    public interface IModificationAction
    {
        string Description { get; }
        FSEntry? Target { get; }
        Task ExecuteAsync();
    }
}
