using System.Text;
using System.Threading.Channels;
using Sunfire.Logging;
using Sunfire.Tui.Models;
using Sunfire.Tui.Terminal;
using Sunfire.Ansi;
using Sunfire.Ansi.Registries;
using Sunfire.Glyph;

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

    private readonly RenderState renderState = new(2048);

    private readonly TimeSpan batchDelay = _batchDelay ?? TimeSpan.FromMicroseconds(100);

    private readonly IWindowResizer windowResizer = WindowResizerFactory.Create();

    private readonly Channel<Func<Task>> renderQueue = Channel.CreateUnbounded<Func<Task>>();

    private readonly List<(int x, int y, int w, int h)> clearTasks = [];
    private readonly List<Func<Task>> postRenderTasks = [];

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

                //Skip render if cancelled basically
                if (runningTasks.Count > 0 && !token.IsCancellationRequested)
                    await OnRender(asb);

                await HandlePostRenderTasks();

                await Logger.Debug(nameof(Tui), $" - (Total:    {(DateTime.Now - renderStartTime).TotalMicroseconds}us)");
            }
            catch (OperationCanceledException) { } //Non-Issue just allow to stop
        }

        await Write(AnsiRegistry.ExitAlternateScreen);
        await Write(AnsiRegistry.ShowCursor);
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
    public void PostRender(Func<Task> task)
    {
        postRenderTasks.Add(task);
    }

    private async Task OnRender(AnsiStringBuilder asb)
    {
        //Rearrange, returns true if anything was changed
        var invalidScreen = await RootView.Arrange();

        //Force invalidation of front buffer
        HandleClearTasks();

        if (!invalidScreen)
            return;

        //Draw to back buffer
        await RootView.Draw(new SVContext(0, 0, _backBuffer));

        //Clear builder, ensure cursor is hidden for draw, reset state
        asb.Clear();
        asb.HideCursor();
        renderState.Reset();

        for (int y = 0; y < RootView.SizeY; y++)
        {
            int x = 0;
            while(x < RootView.SizeX)
            {
                var cell = _backBuffer[x,y];
                
                if(cell == FrontBuffer[x,y])
                {
                    FlushToAsb(asb);

                    x += cell.Width;
                    continue;
                }

                if(cell.StyleId != renderState.CurrentStyleId)
                    FlushToAsb(asb);

                if(renderState.OutputIndex == 0)
                {
                    renderState.CurrentStyleId = cell.StyleId;

                    var cellStyle = StyleFactory.Get(cell.StyleId);
                    renderState.CurrentStyle = cellStyle;

                    renderState.OutputStart = (x,y);
                }

                var cluster = GlyphFactory.Get(cell.GlyphId);
                var text = cluster.GraphemeCluster.AsSpan();

                text.CopyTo(renderState.OutputBuffer.AsSpan(renderState.OutputIndex));
                renderState.OutputIndex += text.Length;
                renderState.CursorMovement += cluster.Width;

                //Add extra space to "Fake" 2 wide characters
                if(cell.Width > 1 && cluster.RealWidth < cell.Width)
                    renderState.OutputBuffer[renderState.OutputIndex++] = ' ';

                x += cell.Width;
            }
            FlushToAsb(asb);
        }
        //Append final escape codes like resetting properties
        asb.ResetProperties();

        await Write(asb.ToString());

        //Swap back buffer to front, clear back buffer
        (_backBuffer, FrontBuffer) = (FrontBuffer, _backBuffer);
        _backBuffer.Clear();
    }


    private void FlushToAsb(AnsiStringBuilder asb)
    {
        if (renderState.OutputIndex == 0)
            return;

        //Change to new style, append text, move cursor if cursor not in the correct place already
        asb.Append(
            renderState.OutputBuffer.AsSpan(0, renderState.OutputIndex), 
            renderState.CurrentStyle,
            renderState.Cursor == renderState.OutputStart ? null : renderState.OutputStart
        );

        renderState.Cursor = (renderState.OutputStart.X + renderState.CursorMovement, renderState.OutputStart.Y);
        renderState.OutputIndex = 0; 
        renderState.CursorMovement = 0;
    }

    private void HandleClearTasks()
    {
        if(clearTasks.Count == 0)
            return;
            
        foreach(var clearTask in clearTasks)
            FrontBuffer.Clear(clearTask);

        clearTasks.Clear();
    }
    
    private async Task HandlePostRenderTasks()
    {
        if(postRenderTasks.Count > 0)
        {
            var startTime = DateTime.Now;

            List<Task> tasks = [];

            foreach(var task in postRenderTasks)
                try { tasks.Add(task()); }
                catch (Exception ex) { await Logger.Error(nameof(Tui), $"Action Failed To Start\n{ex}"); }

            try
            {
                await Task.WhenAll(tasks);
            }
            catch (Exception ex)
            {
                var exs = ex is AggregateException ae ? ae.InnerExceptions : (IEnumerable<Exception>)[ex];
                foreach(var ie in exs)
                    _ = Logger.Error(nameof(Tui), $"Render Task Failed\n{ex}");
            }

            postRenderTasks.Clear();

            var postRenderTime = (DateTime.Now - startTime).TotalMicroseconds;
            await Logger.Debug(nameof(Tui), $" - (Ex-Tasks: {postRenderTime}us)");
        }
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
