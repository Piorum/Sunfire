using System.Text;
using System.Threading.Channels;
using Sunfire.Logging;
using SunfireFramework.Rendering;
using SunfireFramework.Terminal;
using SunfireFramework.Views;
using Sunfire.Ansi;
using Sunfire.Ansi.Models;
using Sunfire.Ansi.Registries;

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

        await Write(AnsiRegistry.EnterAlternateScreen);

        //Invalidate intial layout, start first batch cycle
        await EnqueueAction(RootView.Invalidate);

        //Reused string builder
        AnsiStringBuilder asb = new();

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
                    _ = Logger.Error(nameof(SunfireFramework), $"Render Task Failed\n{ex}");
                }

                //Skip render if cancelled basically
                var startTime = DateTime.Now;
                if (runningTasks.Count > 0 && !token.IsCancellationRequested)
                    await OnRender(asb);
                await Logger.Debug(nameof(SunfireFramework), $"[Render Time] {(DateTime.Now - startTime).TotalMicroseconds}us");
            }
            catch (OperationCanceledException) { } //Non-Issue just allow to stop
        }

        await Write(AnsiRegistry.ExitAlternateScreen);
    }

    public async Task EnqueueAction(Func<Task> action)
    {
        await renderQueue.Writer.WriteAsync(action);
    }

    private async Task OnRender(AnsiStringBuilder asb)
    {
        //Rearrange
        var invalidScreen = await RootView.Arrange();

        //Return if nothing was rearranged
        if (!invalidScreen)
            return;

        await RootView.Draw(new SVContext(0, 0, _backBuffer));

        //Clear colors and string builder
        asb.Clear();
        asb.HideCursor();

        string[] outputBuffer = new string[RootView.SizeX];
        int outputIndex = 0;

        SStyle currentStyle = new(null, null, SAnsiProperty.None, (0, 0));
        (int X, int Y) outputStartPos = (0, 0);
        (int X, int Y) cursorPos = (-1, -1);

        void Flush()
        {
            if (outputIndex > 0)
            {
                var outputData = string.Join("", outputBuffer.AsSpan(0, outputIndex).ToArray());

                asb.Append(outputData, currentStyle with { CursorPosition = cursorPos == outputStartPos ? null : outputStartPos });
                cursorPos = (outputStartPos.X + outputIndex, outputStartPos.Y);

                outputBuffer.AsSpan(0, outputIndex).Clear();
                outputIndex = 0;
            }
        }

        for (int y = 0; y < RootView.SizeY; y++)
        {
            for (int x = 0; x < RootView.SizeX; x++)
            {
                var cell = _backBuffer[x, y];

                //Cell is same as already drawn continue
                if (cell == FrontBuffer[x, y])
                {
                    Flush();
                    continue;
                }

                SStyle cellStyle = new(cell.ForegroundColor, cell.BackgroundColor, cell.Properties, null);

                //Style is the same add to buffer and continue
                if (outputIndex == 0 || cellStyle != currentStyle)
                {
                    Flush();

                    currentStyle = cellStyle;
                    outputStartPos = (x, y);
                    outputBuffer[0] = cell.Data;
                    outputIndex = 1;
                }
                else
                {
                    outputBuffer[outputIndex] = cell.Data;
                    outputIndex++;
                }
            }
            Flush();
        }
        asb.Final();

        //Draw to the screen
        await Write(asb.ToString());

        //Swap back buffer to front, clear back buffer
        (_backBuffer, FrontBuffer) = (FrontBuffer, _backBuffer);
        _backBuffer.Clear();
    }

    public async Task Resize()
    {
        await Logger.Debug(nameof(SunfireFramework), "Resizing");
        await EnqueueAction(async () =>
        {
            var newHeight = Console.BufferHeight;
            var newWidth = Console.BufferWidth;

            //Maybe rendering will be fast enough when properly diffed but needed to remove perceived overlapping
            if (RootView.SizeY > newHeight)
                await Write(AnsiRegistry.ClearScreen);

            //Just reset, resizing behavior is too inconsistent to properly resize the buffer
            FrontBuffer = new(newWidth, newHeight);
            _backBuffer = new(newWidth, newHeight);

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
}
