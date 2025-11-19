using System.Threading.Channels;
using Sunfire.Input.Models;
using Sunfire.Input.DataStructures;
using System.Text;
using Sunfire.Input.Builders;
using Sunfire.Logging;

namespace Sunfire.Input;

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
                
                await Logger.Debug(nameof(Input), $"[Bind Received]");
                
                await Logger.Debug(nameof(Input), evt.Key.InputType switch
                {
                    Enums.InputType.Mouse => $" - (Key: {evt.Key.MouseKey}, Modifiers: {evt.Key.Modifiers})",
                    Enums.InputType.Keyboard => $" - (Key: {evt.Key.KeyboardKey}, Modifiers: {evt.Key.Modifiers})",
                    _ => " - None"
                });
                await Logger.Debug(nameof(Input), evt.Key.InputType switch
                {
                    Enums.InputType.Mouse => (evt.InputData.ScrollDelta is null) switch
                        {
                            true => $" - (X: {evt.InputData.X}, Y: {evt.InputData.Y})",
                            false => $" - (X: {evt.InputData.X}, Y: {evt.InputData.Y}, ScrollDelta: {evt.InputData.ScrollDelta})"
                        },
                    Enums.InputType.Keyboard => $" - (UTFChar: {evt.InputData.UTFChar})",
                    _ => " - None"
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

        //Executing bindings and log errors
        foreach(var bind in bindings)
            _ = ExecuteBinding(bind, inputData);

        return Task.FromResult(bindings.Any());
    }

    private async Task ExecuteBinding(Bind bind, InputData inputData)
    {
        try
        {
            await bind.Task(inputData);
        }
        catch (Exception ex)
        {
            await Logger.Error(nameof(Input), ex.ToString());
        }
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
