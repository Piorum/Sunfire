using Sunfire.Logging.Models;

namespace Sunfire.Logging.Interfaces;

public interface ILogSink
{
    Task WriteAsync(LogMessage message);
    Task Flush();
}
