namespace Sunfire.Common;

public record class SunfireResult(
    bool Success,
    Exception? Exception = null
);
