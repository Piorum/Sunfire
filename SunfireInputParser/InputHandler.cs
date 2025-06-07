using SunfireInputParser.Input;
using System.Threading.Channels;

namespace SunfireInputParser;

public class InputHandler
{
    private readonly IInputTranslator InputTranslator = InputTranslatorFactory.Create();
    private readonly Channel<TerminalInput> inputChannel = Channel.CreateUnbounded<TerminalInput>();

    public async Task Start(CancellationTokenSource? cts = default)
    {
        cts ??= new(); //register own if null to exit

        var pollTask = Task.Run(async () => { await InputTranslator.PollInput(inputChannel.Writer, cts.Token); });

        var handleTask = Task.Run(async () => { await Handle(cts); });

        await Task.WhenAll(pollTask, handleTask);
    }

    private async Task Handle(CancellationTokenSource cts)
    {
        try
        {
            await foreach (var evt in inputChannel.Reader.ReadAllAsync(cts.Token))
            {
                if (evt.Type == Enums.InputType.Keyboard)
                    await KeyboardHandler(evt, cts);
                if (evt.Type == Enums.InputType.Mouse)
                    await MouseHandler(evt, cts);
            }
        }
        catch (OperationCanceledException) { }
    }

    private static Task KeyboardHandler(TerminalInput evt, CancellationTokenSource cts)
    {
        if (evt.Key == ConsoleKey.Q)
        {
            cts.Cancel();
        }
        return Task.CompletedTask;
    }

    private static Task MouseHandler(TerminalInput evt, CancellationTokenSource cts)
    {
        return Task.CompletedTask;
    }
}
