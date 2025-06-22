using Sunfire.Logging.Interfaces;

namespace Sunfire.Logging.Models;

public readonly record struct SinkConfiguration(
    ILogSink Sink,
    HashSet<LogLevel> Levels
);
