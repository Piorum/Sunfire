namespace Sunfire.Logging.Interfaces;

public interface ILogger<T> where T : ILogger<T>
{
    static abstract Task Debug(string provider, string message);
    static abstract Task Info(string provider, string message);
    static abstract Task Warn(string provider, string message);
    static abstract Task Error(string provider, string message);
    static abstract Task Fatal(string provider, string message);
}
