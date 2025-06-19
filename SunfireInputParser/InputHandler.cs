using System.Threading.Channels;
using SunfireInputParser.Types;
using SunfireInputParser.DataStructures;
using System.Text;
using SunfireInputParser.Builders;

namespace SunfireInputParser;

public class InputHandler<TContextEnum> where TContextEnum : struct, Enum
{
    public readonly HashSet<TContextEnum> Context = [];

    internal readonly TrieNode<TContextEnum> sequenceBindsRoot = new();
    internal readonly Dictionary<(Key, TContextEnum), Bind?> indifferentBinds = [];

    private readonly IInputTranslator inputTranslator = InputTranslatorFactory.Create();
    private readonly Channel<TerminalInput> inputChannel = Channel.CreateUnbounded<TerminalInput>();

    private TrieNode<TContextEnum> currentNode;
    private readonly List<Key> currentSequence = [];

    private readonly System.Timers.Timer sequenceTimeoutTimer;

    private CancellationTokenSource? _cts;

    public InputHandler(int sequenceTimeoutMs = 1000)
    {
        currentNode = sequenceBindsRoot;

        sequenceTimeoutTimer = new(sequenceTimeoutMs);

        sequenceTimeoutTimer.Elapsed += (source, e) =>
        {
            Task.Run(() => ResetSequence());
        };

        sequenceTimeoutTimer.AutoReset = true;
        sequenceTimeoutTimer.Enabled = true;
    }

    public KeybindBuilder<TContextEnum> CreateBinding()
    {
        return new(this);
    }

    public async Task Start(CancellationTokenSource? cts = default)
    {
        _cts = cts ?? new();

        var pollTask = Task.Run(() => inputTranslator.PollInput(inputChannel.Writer, _cts.Token));

        var handleTask = Task.Run(() => Handle(_cts));

        await Task.WhenAll(pollTask, handleTask);
    }

    public async Task Stop()
    {
        if (_cts is not null)
        {
            await _cts.CancelAsync();
            _cts.Dispose();
        }
    }

    public Task<string?> SequenceText()
    {
        if (currentSequence.Count > 0)
        {
            StringBuilder sb = new();
            foreach (var key in currentSequence)
            {
                if (sb.Length > 0)
                    sb.Append(", ");
                sb.Append('\'');

                if (key.Modifiers.HasFlag(Enums.Modifier.Ctrl))
                    sb.Append("Ctrl+");
                if (key.Modifiers.HasFlag(Enums.Modifier.Shift))
                    sb.Append("Shift+");
                if (key.Modifiers.HasFlag(Enums.Modifier.Alt))
                    sb.Append("Alt+");
                switch (key.InputType)
                {
                    case Enums.InputType.Keyboard:
                        sb.Append($"{key.KeyboardKey}'");
                        break;
                    case Enums.InputType.Mouse:
                        sb.Append($"{key.MouseKey}'");
                        break;
                }
            }

            return Task.FromResult<string?>(sb.ToString());
        }
        else
        {
            return Task.FromResult<string?>(null);
        }
    }

    private async Task Handle(CancellationTokenSource cts)
    {
        try
        {
            await foreach (var evt in inputChannel.Reader.ReadAllAsync(cts.Token))
            {
                var indifferentBindsTask = Task.Run(async () =>
                {
                    await ExecuteBindings(
                        indifferentBinds,
                        ctx => (evt.Key, ctx),
                        evt.InputData
                    );
                });

                var sequenceBindsTask = Task.Run(async () =>
                {
                    //Reset timeout
                    await ResetTimer();

                    currentNode.Children.TryGetValue(evt.Key, out var node);

                    //Not valid keybind sequence
                    if (node is null)
                    {
                        await ResetSequence();
                        return;
                    }

                    currentNode = node;
                    currentSequence.Add(evt.Key);

                    //No binds at this node
                    if (!node.IsTerminal)
                        return;

                    var executedBinds = await ExecuteBindings(
                        node.Bindings,
                        ctx => ctx,
                        evt.InputData
                    );

                    if(executedBinds)
                        await ResetSequence();
                });

                await Task.WhenAll(indifferentBindsTask, sequenceBindsTask);
            }
        }
        catch (OperationCanceledException) { }
    }

    private Task<bool> ExecuteBindings<TKey>(Dictionary<TKey, Bind?> dictionary, Func<TContextEnum, TKey> keySelector, InputData inputData) where TKey : notnull
    {
        //Select all unique binds where context matches
        var bindings = Context
            .Select(ctx => dictionary.TryGetValue(keySelector(ctx), out var bind) ? bind : default)
            .Where(bind => bind is not null)
            .Select(bind => bind!.Value)
            .DistinctBy(bind => bind.Id);

        //Fire and forget bound tasks
        _ = bindings
            .Select(bind => bind.Task(inputData))
            .ToList();

        return Task.FromResult(bindings.Any());
    }

    private Task ResetSequence()
    {
        currentNode = sequenceBindsRoot;
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
