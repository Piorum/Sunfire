using System.Text;
using SunfireFramework.Rendering;
using SunfireFramework.Terminal;
using SunfireFramework.Views;

namespace SunfireFramework;

public class Renderer(RootSV rootView, int? refreshRate = null)
{
    public readonly RootSV RootView = rootView;

    private SVBuffer _frontBuffer = new(rootView.SizeX, rootView.SizeY);
    private SVBuffer _backBuffer = new(rootView.SizeX, rootView.SizeY);

    private readonly TimeSpan frameTimeTarget = TimeSpan.FromSeconds(1.0 / refreshRate ?? 60);

    private readonly IWindowResizer windowResizer = WindowResizerFactory.Create();

    private static readonly Stream s_stdout = Console.OpenStandardOutput();
    private static readonly UTF8Encoding s_utf8Encoder = new(false);

    public async Task Render(CancellationToken token)
    {
        if (!windowResizer.Registered)
            await windowResizer.RegisterResizeEvent(this);

        await RootView.Arrange();

        StringBuilder sb = new();
        SVColor? currentForeground = null;
        SVColor? currentBackground = null;

        await Write(HideCursor);

        (double total, int count) drawLog = (0, 0);
        (double total, int count) renderLog = (0, 0);

        Task writeTask = Task.CompletedTask;
        while (!token.IsCancellationRequested)
        {
            var startTime = DateTime.Now;
            await RootView.Draw(new SVContext(0, 0, _backBuffer));
            var drawTime = DateTime.Now - startTime;

            drawLog.count++;
            drawLog.total += drawTime.TotalMicroseconds;

            sb.Clear();

            sb.Append(MoveCursor(0, 0));
            for (int y = 0; y < RootView.SizeY; y++)
            {
                for (int x = 0; x < RootView.SizeX; x++)
                {
                    var cell = _backBuffer[x, y];

                    if (cell.ForegroundColor != currentForeground)
                    {
                        currentForeground = cell.ForegroundColor;
                        sb.Append(SetColor(cell.ForegroundColor, true));
                    }
                    if (cell.BackgroundColor != currentBackground)
                    {
                        currentBackground = cell.BackgroundColor;
                        sb.Append(SetColor(cell.BackgroundColor, false));
                    }

                    sb.Append(cell.Char);
                }
                if (y < RootView.SizeY - 1) sb.Append('\n');
            }
            sb.Append(Reset);
            await writeTask;
            writeTask = Write(sb.ToString());

            (_backBuffer, _frontBuffer) = (_frontBuffer, _backBuffer);

            //Try to match given frame time target but don't exceed it
            var renderTime = DateTime.Now - startTime;

            renderLog.count++;
            renderLog.total += renderTime.TotalMicroseconds - drawTime.TotalMicroseconds;

            var remainingTime = frameTimeTarget - renderTime;
            var delay = Math.Max((int)remainingTime.TotalMilliseconds, 0);

            try
            {
                var clearTask = Task.Run(() => _backBuffer.Clear(), token);

                await Task.Delay(delay, token);
                await clearTask;
            }
            catch (OperationCanceledException) { }
        }

        await SVLogger.LogMessage($"Avg Draw Time : {drawLog.total / drawLog.count}");
        await SVLogger.LogMessage($"Avg Render Time : {renderLog.total / renderLog.count}");
    }

    public async Task Resize()
    {
        RootView.SizeX = Console.BufferWidth;
        RootView.SizeY = Console.BufferHeight;

        _frontBuffer = new(RootView.SizeX, RootView.SizeY);
        _backBuffer = new(RootView.SizeX, RootView.SizeY);

        await RootView.Arrange();
    }

    public static async Task Write(string text)
    {
        if (string.IsNullOrEmpty(text)) return;

        byte[] bytes = s_utf8Encoder.GetBytes(text);

        await s_stdout.WriteAsync(bytes);
        await s_stdout.FlushAsync();
    }

    public static string MoveCursor(int line, int column) =>
        $"\x1B[{line + 1};{column + 1}H";
    public static string SetColor(SVColor color, bool foreground) =>
        $"\x1B[{(foreground ? 38 : 48)};2;{color.R};{color.G};{color.B}m";

    public const string Reset = "\x1B[0m";
    public const string HideCursor = "\x1B[?25l";
    public const string ShowCursor = "\x1B[?25l";
}
