using System.Threading.Channels;
using Sunfire.Input.Enums;
using Sunfire.Input.Models;

namespace Sunfire.Input.Platforms.Fallback;

public class FallbackInputTranslator : IInputTranslator
{
    public async Task PollInput(ChannelWriter<TerminalInput> writer, CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            try
            {
                var key = await ReadKeyAsync(token);
                var inputEvent = await Translate(key);

                await writer.WriteAsync(inputEvent, token);
            }
            catch (OperationCanceledException) { }
        }
    }

    private static Task<ConsoleKeyInfo> ReadKeyAsync(CancellationToken token)
    {
        var tcs = new TaskCompletionSource<ConsoleKeyInfo>();
        var cancellationRegistration = token.Register(() => tcs.TrySetCanceled(token));

        _ = Task.Run(() =>
        {
            try
            {
                var key = Console.ReadKey(true);

                tcs.TrySetResult(key);
            }
            catch (InvalidOperationException)
            {
                tcs.TrySetCanceled();
            }
        }, CancellationToken.None);

        return tcs.Task;
    }

    private static Task<TerminalInput> Translate(ConsoleKeyInfo keyInfo)
    {
        var terminalInput = TerminalInput.KeyboardInput(keyInfo.Key, keyInfo.KeyChar, (Modifier)keyInfo.Modifiers);

        return Task.FromResult(terminalInput);
    }
}
