using System.Threading.Channels;
using Sunfire.Input.Enums;
using Sunfire.Input.Types;

namespace Sunfire.Input.Platforms;

public class LinuxInputTranslator : IInputTranslator
{
    public async Task PollInput(ChannelWriter<TerminalInput> writer, CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            if (Console.KeyAvailable)
            {
                var key = Console.ReadKey(true);

                var inputEvent = await Translate(key);

                try
                {
                    await writer.WriteAsync(inputEvent, token);
                }
                catch (OperationCanceledException) { }
            }
            else
            {
                try
                {
                    await Task.Delay(10, token);
                }
                catch (OperationCanceledException) { }
            }
        }
    }

    private static Task<TerminalInput> Translate(ConsoleKeyInfo keyInfo)
    {
        var terminalInput = TerminalInput.KeyboardInput(keyInfo.Key, keyInfo.KeyChar, (Modifier)keyInfo.Modifiers);

        return Task.FromResult(terminalInput);
    }
}
