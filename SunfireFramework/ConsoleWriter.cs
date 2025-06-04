
using System.Text;

namespace SunfireFramework;

public static class ConsoleWriter
{
    private static readonly Lock ConsoleWriterLock = new();
    private static readonly StringBuilder _errorLog = new();

    public static Task WriteAsync(ConsoleOutput output, ConsoleColor? foregroundColor = null, ConsoleColor? backgroundColor = null)
    {
        lock (ConsoleWriterLock)
        {
            var prevFg = Console.ForegroundColor;
            var prevBg = Console.BackgroundColor;

            Console.ForegroundColor = foregroundColor ?? prevFg;
            Console.BackgroundColor = backgroundColor ?? prevBg;

            Console.SetCursorPosition(output.X, output.Y);
            Console.Write(output.Output);

            Console.ForegroundColor = prevFg;
            Console.BackgroundColor = prevBg;
        }
        return Task.CompletedTask;
    }

    public static Task WriteAsync(List<ConsoleOutput> outputs, ConsoleColor? foregroundColor = null, ConsoleColor? backgroundColor = null)
    {
        lock (ConsoleWriterLock)
        {
            var prevFg = Console.ForegroundColor;
            var prevBg = Console.BackgroundColor;

            Console.ForegroundColor = foregroundColor ?? prevFg;
            Console.BackgroundColor = backgroundColor ?? prevBg;

            foreach (var output in outputs)
            {
                Console.SetCursorPosition(output.X, output.Y);
                Console.Write(output.Output);
            }

            Console.ForegroundColor = prevFg;
            Console.BackgroundColor = prevBg;
        }
        return Task.CompletedTask;
    }

    public static Task LogError(string errorMessage)
    {
        _errorLog.AppendLine(errorMessage);
        return Task.CompletedTask;
    }

    public static async Task OutputErrorLog()
    {
        await Console.Error.WriteAsync($"{_errorLog}");
    }
}
