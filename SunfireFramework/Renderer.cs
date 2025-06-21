using System.Text;
using System.Threading.Channels;
using SunfireFramework.Enums;
using SunfireFramework.Rendering;
using SunfireFramework.Terminal;
using SunfireFramework.Views;

namespace SunfireFramework;

public class Renderer(RootSV rootView, TimeSpan? _batchDelay = null)
{
    public readonly RootSV RootView = rootView;

    public SVBuffer FrontBuffer { internal set; get; } = new(rootView.SizeX, rootView.SizeY);
    private SVBuffer _backBuffer = new(rootView.SizeX, rootView.SizeY);

    private readonly TimeSpan batchDelay = _batchDelay ?? TimeSpan.FromMicroseconds(100);

    private readonly IWindowResizer windowResizer = WindowResizerFactory.Create();

    private static readonly Stream s_stdout = Console.OpenStandardOutput();
    private static readonly UTF8Encoding s_utf8Encoder = new(false);

    private readonly Channel<Func<Task>> renderQueue = Channel.CreateUnbounded<Func<Task>>();

    public async Task Start(CancellationToken token)
    {
        //Register resize event
        if (!windowResizer.Registered)
            await windowResizer.RegisterResizeEvent(this);

        await Write(EnterAlternateScreen);

        //Invalidate intial layout, start first batch cycle
        await EnqueueAction(RootView.Invalidate);

        //Reused string builder
        StringBuilder sb = new();

        List<Task> runningTasks = [];
        while (!token.IsCancellationRequested)
        {
            try
            {
                //Clear runningTasks
                runningTasks.Clear();

                //Get first action and start batch timer
                var firstAction = await renderQueue.Reader.ReadAsync(token);
                runningTasks.Add(firstAction());

                var batchTimer = Task.Delay(batchDelay, token);

                //Process more actions while batch timer is not over
                while (true)
                {
                    //Read any available actions
                    while (renderQueue.Reader.TryRead(out var action))
                        runningTasks.Add(action());

                    //Wait for more actions or for batch timer to end
                    var waitForMoreActions = renderQueue.Reader.WaitToReadAsync(token).AsTask();
                    var completedTask = await Task.WhenAny(waitForMoreActions, batchTimer);

                    //Exit if batch timer is done, loop if not (more actions were queued)
                    if (completedTask == batchTimer)
                        break;
                }

                try
                {
                    await Task.WhenAll(runningTasks);
                }
                catch (OperationCanceledException) { } //Non-Issue just allow to stop
                catch (Exception ex)
                {
                    await SVLogger.LogMessage($"Render Task Failed:\n{ex}");
                }

                //Skip render if cancelled basically
                if (runningTasks.Count > 0 && !token.IsCancellationRequested)
                    await OnRender(sb);
            }
            catch (OperationCanceledException) { } //Non-Issue just allow to stop
        }

        await Write(ExitAlternateScreen);
    }

    public async Task EnqueueAction(Func<Task> action)
    {
        await renderQueue.Writer.WriteAsync(action);
    }

    private async Task OnRender(StringBuilder sb)
    {
        //Rearrange
        var invalidScreen = await RootView.Arrange();

        //Return if nothing was rearranged
        if (!invalidScreen)
            return;

        await RootView.Draw(new SVContext(0, 0, _backBuffer));

        //Clear colors and string builder
        sb.Clear();
        //Need to change pretty much all of the following code to properly diff the buffers and minimize output
        SVColor? currentForeground = null;
        SVColor? currentBackground = null;
        SVTextProperty? currentProperties = null;

        sb.Append(HideCursor);
        sb.Append(MoveCursor(0, 0));
        sb.Append(ResetForegroundColor);
        sb.Append(ResetBackgroundColor);

        for (int y = 0; y < RootView.SizeY; y++)
        {
            for (int x = 0; x < RootView.SizeX; x++)
            {
                var cell = _backBuffer[x, y];

                if (cell.Properties != currentProperties)
                {
                    if (cell.Properties.HasFlag(SVTextProperty.Highlight) && ((currentProperties.HasValue && !currentProperties.Value.HasFlag(SVTextProperty.Highlight)) || !currentProperties.HasValue))
                    {
                        sb.Append(ReverseVideoMode);
                    }
                    else if (!cell.Properties.HasFlag(SVTextProperty.Highlight) && currentProperties.HasValue && currentProperties.Value.HasFlag(SVTextProperty.Highlight))
                    {
                        sb.Append(DisableReverseVideoMode);
                    }

                    currentProperties = cell.Properties;
                }
                if (cell.ForegroundColor != currentForeground || cell.BackgroundColor != currentBackground)
                {
                    if (cell.ForegroundColor != currentForeground)
                    {
                        currentForeground = cell.ForegroundColor;
                        sb.Append(cell.ForegroundColor is null ? ResetForegroundColor : SetColor((SVColor)cell.ForegroundColor, true));
                    }
                    if (cell.BackgroundColor != currentBackground)
                    {
                        currentBackground = cell.BackgroundColor;
                        sb.Append(cell.BackgroundColor is null ? ResetBackgroundColor : SetColor((SVColor)cell.BackgroundColor, false));
                    }
                }

                sb.Append(cell.Data);
            }
            if (y < RootView.SizeY - 1) sb.Append('\n');
        }

        sb.Append(Reset);
        sb.Append(ShowCursor);

        //Draw to the screen
        await Write(sb.ToString());

        //Swap back buffer to front, clear back buffer
        (_backBuffer, FrontBuffer) = (FrontBuffer, _backBuffer);
        _backBuffer.Clear();
    }

    public async Task Resize()
    {
        await EnqueueAction(async () =>
        {
            var newHeight = Console.BufferHeight;
            var newWidth = Console.BufferWidth;

            //Maybe rendering will be fast enough when properly diffed but needed to remove perceived overlapping
            if (RootView.SizeY > newHeight)
                await Write(ClearScreen);

            FrontBuffer.Resize(newWidth, newHeight); //Resize for comparison
            _backBuffer = new(newWidth, newHeight); //Back buffer will be fully redrawn anyways

            RootView.SizeX = newWidth;
            RootView.SizeY = newHeight;

            await RootView.Invalidate();
        });
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
    public const string ResetForegroundColor = "\x1b[39m";
    public const string ResetBackgroundColor = "\x1b[49m";
    public const string ReverseVideoMode = "\x1b[7m";
    public const string DisableReverseVideoMode = "\x1b[27m";
    public const string HideCursor = "\x1B[?25l";
    public const string ShowCursor = "\x1B[?25l";
    public const string EnterAlternateScreen = "\x1b[?1049h";
    public const string ExitAlternateScreen = "\x1b[?1049l";
    public const string ClearScreen = "\x1b[2J";
}
