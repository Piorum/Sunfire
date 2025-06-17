using System.Text;

namespace SunfireFramework.Terminal;

public static class TerminalWriter
{
    private static readonly Lock ConsoleWriterLock = new();
    private static readonly StringBuilder _errorLog = new();

    public static Task WriteAsync(TerminalOutput output, ConsoleColor? foregroundColor = null, ConsoleColor? backgroundColor = null)
    {
        lock (ConsoleWriterLock)
        {
            var prevFg = Console.ForegroundColor;
            var prevBg = Console.BackgroundColor;

            //Console.ForegroundColor = foregroundColor ?? prevFg;
            //Console.BackgroundColor = backgroundColor ?? prevBg;

            //Console.SetCursorPosition(output.X, output.Y);
            //Console.Write(output.Output);

            //Console.ForegroundColor = prevFg;
            //Console.BackgroundColor = prevBg;
        }
        return Task.CompletedTask;
    }

    public static Task WriteAsync(List<TerminalOutput> outputs, ConsoleColor? foregroundColor = null, ConsoleColor? backgroundColor = null)
    {
        lock (ConsoleWriterLock)
        {
            var prevFg = Console.ForegroundColor;
            var prevBg = Console.BackgroundColor;

            //Console.ForegroundColor = foregroundColor ?? prevFg;
            //Console.BackgroundColor = backgroundColor ?? prevBg;

            foreach (var output in outputs)
            {
                //Console.SetCursorPosition(output.X, output.Y);
                //Console.Write(output.Output);
            }

            //Console.ForegroundColor = prevFg;
            //Console.BackgroundColor = prevBg;
        }
        return Task.CompletedTask;
    }

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
