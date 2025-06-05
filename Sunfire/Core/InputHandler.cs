namespace Sunfire.Core;

public static class InputHandler
{
    public static Task InputLoop(CancellationTokenSource cts)
    {
        while (!cts.Token.IsCancellationRequested)
        {
            if (Console.KeyAvailable)
            {
                var keyInfo = Console.ReadKey(true);
                if (Keybindings.Equals(keyInfo, Keybindings.ExitKey))
                    cts.Cancel();
            }
        }
        return Task.CompletedTask;
    }

}
