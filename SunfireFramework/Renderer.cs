using System.Text;
using SunfireFramework.Rendering;
using SunfireFramework.Terminal;
using SunfireFramework.Views;

namespace SunfireFramework;

public class Renderer(RootSV rootView, TimeSpan? _batchDelay = null)
{
    public readonly RootSV RootView = rootView;

    private SVBuffer _frontBuffer = new(rootView.SizeX, rootView.SizeY);
    private SVBuffer _backBuffer = new(rootView.SizeX, rootView.SizeY);

    private readonly TimeSpan batchDelay = _batchDelay ?? TimeSpan.FromMicroseconds(500);

    private readonly IWindowResizer windowResizer = WindowResizerFactory.Create();

    private static readonly Stream s_stdout = Console.OpenStandardOutput();
    private static readonly UTF8Encoding s_utf8Encoder = new(false);

    //private readonly Channel<TerminalInput> inputChannel = Channel.CreateUnbounded<TerminalInput>();

    public async Task Render(CancellationToken token)
    {
        //Register resize event
        if (!windowResizer.Registered)
            await windowResizer.RegisterResizeEvent(this);

        //Invalidate Initial Layout
        await RootView.Invalidate();

        //Reused object/structs
        StringBuilder sb = new();
        SVColor? currentForeground = null;
        SVColor? currentBackground = null;

        //Delay task that determines how longer 
        async Task Delay()
        {
            try
            {
                await Task.Delay(batchDelay, token);
            }
            catch (OperationCanceledException) { }
        }

        Task writeTask = Task.CompletedTask;
        while (!token.IsCancellationRequested)
        {
            var invalidScreen = await RootView.Arrange();

            if (!invalidScreen)
            {
                await Delay();
                continue;
            }

            await RootView.Draw(new SVContext(0, 0, _backBuffer));

            //Clear colors and string builder
            sb.Clear();
            currentForeground = null;
            currentBackground = null;

            sb.Append(HideCursor);
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
            sb.Append(ShowCursor);

            await writeTask;
            writeTask = Write(sb.ToString());

            (_backBuffer, _frontBuffer) = (_frontBuffer, _backBuffer);
            _backBuffer.Clear();
        }
    }

    public async Task Resize()
    {
        RootView.SizeX = Console.BufferWidth;
        RootView.SizeY = Console.BufferHeight;

        _frontBuffer = new(RootView.SizeX, RootView.SizeY);
        _backBuffer = new(RootView.SizeX, RootView.SizeY);

        await RootView.Invalidate();
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
