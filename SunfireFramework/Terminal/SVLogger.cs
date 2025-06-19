using System.Text;

namespace SunfireFramework.Terminal;

public static class SVLogger
{
    private static readonly StringBuilder _errorLog = new();

    public static Task LogMessage(string errorMessage)
    {
        _errorLog.AppendLine(errorMessage);
        return Task.CompletedTask;
    }

    public static async Task OutputLog()
    {
        await Console.Error.WriteAsync($"{_errorLog}");
    }
}
