using System.Text;
using System.Threading.Channels;
using Sunfire.Logging;
using Sunfire.Tui.Models;
using Sunfire.Tui.Terminal;
using Sunfire.Ansi;
using Sunfire.Ansi.Models;
using Sunfire.Ansi.Registries;

namespace Sunfire.Tui;

/// <summary>
/// Handles outputting data requested by given RootView
/// </summary>
/// <param name="rootView">RootView which all other views should come from.</param>
/// <param name="_batchDelay">Amount of time renderer spends waiting for more render actions before rendering, by default 100μs</param>
public class Renderer(RootSV rootView, TimeSpan? _batchDelay = null)
{
    private static readonly Stream s_stdout = Console.OpenStandardOutput();
    private static readonly UTF8Encoding s_utf8Encoder = new(false);

    public readonly RootSV RootView = rootView;

    public SVBuffer FrontBuffer { internal set; get; } = new(rootView.SizeX, rootView.SizeY);
    private SVBuffer _backBuffer = new(rootView.SizeX, rootView.SizeY);

    private readonly TimeSpan batchDelay = _batchDelay ?? TimeSpan.FromMicroseconds(100);

    private readonly IWindowResizer windowResizer = WindowResizerFactory.Create();

    private readonly Channel<Func<Task>> renderQueue = Channel.CreateUnbounded<Func<Task>>();

    private readonly List<(int x, int y, int w, int h)> clearTasks = [];

    /// <summary>
    /// Starts rendering loop.
    /// </summary>
    public async Task Start(CancellationToken token)
    {
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

                runningTasks.Clear();

                //Get first action and start batch timer
                var firstAction = await renderQueue.Reader.ReadAsync(token);

                await Logger.Debug(nameof(Tui), $"[Starting New Render Cycle]");
                var renderStartTime = DateTime.Now;

                try { runningTasks.Add(firstAction()); }
                catch (Exception ex) { await Logger.Error(nameof(Tui), $"Action Failed To Start\n{ex}"); }

                var batchTimer = Task.Delay(batchDelay, token);

                //Process more actions while batch timer is not over
                while (true)
                {
                    //Read any available actions
                    while (renderQueue.Reader.TryRead(out var action))
                        try { runningTasks.Add(action()); }
                        catch (Exception ex) { await Logger.Error(nameof(Tui), $"Action Failed To Start\n{ex}"); }

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
                catch (Exception ex)
                {
                    var exs = ex is AggregateException ae ? ae.InnerExceptions : (IEnumerable<Exception>)[ex];
                    foreach(var ie in exs)
                        _ = Logger.Error(nameof(Tui), $"Render Task Failed\n{ex}");
                }

                await Logger.Debug(nameof(Tui), $" - (Tasks:    {(DateTime.Now - renderStartTime).TotalMicroseconds}us)");

                //Skip render if cancelled basically
                if (runningTasks.Count > 0 && !token.IsCancellationRequested)
                    await OnRender(asb);

                await Logger.Debug(nameof(Tui), $" - (Total:    {(DateTime.Now - renderStartTime).TotalMicroseconds}us)");
            }
            catch (OperationCanceledException) { } //Non-Issue just allow to stop
        }

        await Write(AnsiRegistry.ExitAlternateScreen);
    }

    /// <summary>
    /// Used to Enqueue a task to the render queue.
    /// </summary>
    public async Task EnqueueAction(Func<Task> action)
    {
        await renderQueue.Writer.WriteAsync(action);
    }

    public void Clear(int x, int y, int w, int h)
    {
        clearTasks.Add((x, y, w, h));
    }

    private async Task OnRender(AnsiStringBuilder asb)
    {
        DateTime startTime;

        //Rearrange, returns true if anything was changed
        startTime = DateTime.Now;
        var invalidScreen = await RootView.Arrange();
        var arrangeTime = (DateTime.Now - startTime).TotalMicroseconds;

        if(clearTasks.Count > 0)
        {
            foreach(var clearTask in clearTasks)
            {
                asb.Clear();
                asb.HideCursor();

                var (x, y, w, h) = clearTask;

                var blankString = new string(' ', w);
                for(int i = y; i < h; i++)
                {
                    asb.Append(blankString, new( CursorPosition: (x, i) ));
                    for(int j = x; j < w; j++)
                    {
                        FrontBuffer[j, i] = SVCell.Blank;
                    }
                }

                asb.ResetProperties();
                await Write(asb.ToString());
            }

            clearTasks.Clear();
        }

        if (!invalidScreen)
            return;

        startTime = DateTime.Now;
        await RootView.Draw(new SVContext(0, 0, _backBuffer));
        var drawTime = (DateTime.Now - startTime).TotalMicroseconds;

        //Clear builder, ensure cursor is hidden for draw
        startTime = DateTime.Now;
        asb.Clear();
        asb.HideCursor();

        char[] outputBuffer = new char[RootView.SizeX];
        int outputIndex = 0;

        SStyle currentStyle = new(null, null, SAnsiProperty.None, (0, 0));
        //These need to be different or redraw breaks
        (int X, int Y) outputStartPos = (0, 0);
        (int X, int Y) cursorPos = (-1, -1);

        //Function that outputs the buffer to the screen and clears
        void Flush()
        {
            if (outputIndex > 0)
            {
                var outputData = string.Join("", outputBuffer.AsSpan(0, outputIndex).ToArray());

                //Change to new style, append text, move cursor if cursor not in the correct place already
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
                    //Since we don't know if this is the first cell that isn't being added to the buffer, just ensure buffer is cleared
                    Flush();
                    continue;
                }

                SStyle cellStyle = new(cell.ForegroundColor, cell.BackgroundColor, cell.Properties, null);

                //Style is NOT the same or buffer is empty, clear buffer and add first value
                if (outputIndex == 0 || cellStyle != currentStyle)
                {
                    Flush();

                    currentStyle = cellStyle;
                    outputStartPos = (x, y);
                    outputBuffer[0] = cell.Data;
                    outputIndex = 1;
                }
                //Style is the same add to buffer, inc, continue
                else
                {
                    outputBuffer[outputIndex] = cell.Data;
                    outputIndex++;
                }
            }
            //Move command will be sent to each row to ensure consistently
            //End of row ensure current buffer is output and cleared
            Flush();
        }
        //Append final escape codes like resetting properties
        asb.ResetProperties();
        var buildTime = (DateTime.Now - startTime).TotalMicroseconds;

        startTime = DateTime.Now;
        await Write(asb.ToString());
        var writeTime = (DateTime.Now - startTime).TotalMicroseconds;

        startTime = DateTime.Now;
        //Swap back buffer to front, clear back buffer
        (_backBuffer, FrontBuffer) = (FrontBuffer, _backBuffer);
        _backBuffer.Clear();
        var swapTime = (DateTime.Now - startTime).TotalMicroseconds;

        await Logger.Debug(nameof(Tui), $" - (Arrange:  {arrangeTime}us)");
        await Logger.Debug(nameof(Tui), $" - (Draw:     {drawTime}us)");
        await Logger.Debug(nameof(Tui), $" - (Build     {buildTime}us)");
        await Logger.Debug(nameof(Tui), $" - (Write:    {writeTime}us)");
        await Logger.Debug(nameof(Tui), $" - (Swap:     {swapTime}us)");
    }

    private static async Task Write(string text)
    {
        if (string.IsNullOrEmpty(text)) return;

        byte[] bytes = s_utf8Encoder.GetBytes(text);

        await s_stdout.WriteAsync(bytes);
        await s_stdout.FlushAsync();
    }

    internal async Task Resize()
    {
        await EnqueueAction(async () =>
        {
            await Write(AnsiRegistry.ClearScreen);

            var newHeight = Console.BufferHeight;
            var newWidth = Console.BufferWidth;
            await Logger.Debug(nameof(Tui), $"[Resizing] ({RootView.SizeX},{RootView.SizeY}) -> ({newWidth},{newHeight})");

            //Just reset, resizing behavior is too inconsistent to properly resize the buffer
            FrontBuffer = new(newWidth, newHeight);
            _backBuffer = new(newWidth, newHeight);

            RootView.SizeX = newWidth;
            RootView.SizeY = newHeight;

            await RootView.Invalidate();
        });
    }
}
