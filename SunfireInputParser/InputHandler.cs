using System.Threading.Channels;
using SunfireInputParser.Types;
using SunfireInputParser.DataStructures;

namespace SunfireInputParser;

public class InputHandler<TContextEnum> where TContextEnum : struct, Enum
{
    public readonly HashSet<TContextEnum> Context = [];

    private readonly IInputTranslator inputTranslator = InputTranslatorFactory.Create();
    private readonly Channel<TerminalInput> inputChannel = Channel.CreateUnbounded<TerminalInput>();

    internal readonly TrieNode<TContextEnum> bindingRootNode = new();

    private TrieNode<TContextEnum> currentNode;
    private readonly List<Key> currentSequence = [];

    private readonly System.Timers.Timer sequenceTimeoutTimer;

    public InputHandler(int sequenceTimeoutMs = 1000)
    {
        currentNode = bindingRootNode;

        sequenceTimeoutTimer = new(sequenceTimeoutMs);

        sequenceTimeoutTimer.Elapsed += (source, e) =>
        {
            Task.Run(async () =>
            {
                await ResetSequence();
            });
        };

        sequenceTimeoutTimer.AutoReset = true;
        sequenceTimeoutTimer.Enabled = true;
    }

    public async Task Start(CancellationTokenSource? cts = default)
    {
        cts ??= new(); //register own if null to exit

        var pollTask = Task.Run(async () => { await inputTranslator.PollInput(inputChannel.Writer, cts.Token); });

        var handleTask = Task.Run(async () => { await Handle(cts); });

        await Task.WhenAll(pollTask, handleTask);
    }

    private async Task Handle(CancellationTokenSource cts)
    {
        try
        {
            await foreach (var evt in inputChannel.Reader.ReadAllAsync(cts.Token))
            {
                await ResetTimer();

                currentNode.Children.TryGetValue(evt.Key, out var node);

                if (node is null)
                {
                    await ResetSequence();
                    continue;
                }

                if (!node.IsTerminal)
                {
                    currentNode = node;
                    continue;
                }

                foreach (var context in Context)
                {
                    node.Bindings.TryGetValue(context, out var binding);

                    if (binding is null)
                        continue;

                    _ = binding(evt.InputData);
                }
                await ResetSequence();
            }
        }
        catch (OperationCanceledException) { }
    }

    private Task ResetSequence()
    {
        currentNode = bindingRootNode;
        currentSequence.Clear();
        return Task.CompletedTask;
    }

    private Task ResetTimer()
    {
        sequenceTimeoutTimer.Stop();
        sequenceTimeoutTimer.Start();
        return Task.CompletedTask;
    }
}
