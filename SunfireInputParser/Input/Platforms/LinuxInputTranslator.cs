using System.Threading.Channels;
using SunfireInputParser.Enums;

namespace SunfireInputParser.Input.Platforms;

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
                catch (OperationCanceledException) {}
            }
        }
    }

    private static Task<TerminalInput> Translate(ConsoleKeyInfo keyInfo)
    {
        var terminalInput = new TerminalInput
        {
            Type = InputType.Keyboard,
            CreationTime = DateTime.Now,

            Key = keyInfo.Key,
            Char = keyInfo.KeyChar,
            Modifiers = (Modifier)keyInfo.Modifiers
        };

        return Task.FromResult(terminalInput);
    }
}
