namespace Sunfire;

internal class Program
{
    public static readonly CancellationTokenSource _cts = new();
    public static readonly ManualResetEventSlim _renderSignal = new();

    public static async Task Main()
    {
        Console.CursorVisible = false;
        Console.Clear();
        await Console.Out.WriteAsync("Hello World!");

        var inputTask = Task.Run(InputLoop);
        var renderTask = Task.Run(RenderLoop);

        await Task.WhenAll(inputTask, renderTask);

        Console.Clear();
    }

    private static Task InputLoop()
    {
        while (!_cts.Token.IsCancellationRequested)
        {
            var keyInfo = Console.ReadKey(true);
            if (Keybindings.Equals(keyInfo, Keybindings.ExitKey))
                _cts.Cancel();
        }
        return Task.CompletedTask;
    }

    private static Task RenderLoop()
    {
        while (!_cts.Token.IsCancellationRequested)
        {
            try
            {
                _renderSignal.Wait(_cts.Token);
            }
            catch (OperationCanceledException)
            {
                return Task.CompletedTask;
            }
        }
        return Task.CompletedTask;
    }
}