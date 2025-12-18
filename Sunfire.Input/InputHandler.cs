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

    private readonly Channel<string> sequenceTextChannel = Channel.CreateUnbounded<string>();
    public ChannelReader<string> SequenceTextReader => sequenceTextChannel.Reader;

    private TrieNode<TContextEnum> currentNode;
    private readonly List<Key> currentSequence = [];

    private readonly System.Timers.Timer sequenceTimeoutTimer;

    private bool textMode = false;
    private readonly Dictionary<ConsoleKey, Func<Task>> _textKeyHandlers = [];
    private Func<char, Task>? _textHandler;

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

    public async Task Init(CancellationToken token)
    {
        var pollTask = Task.Run(() => inputTranslator.PollInput(inputChannel.Writer, token), CancellationToken.None);

        var handleTask = Task.Run(() => Handle(token), CancellationToken.None);

        await Task.WhenAll(pollTask, handleTask);
    }

    public void EnableInputMode(Func<char, Task> textHandler, List<(ConsoleKey, Func<Task>)> exitHandlers, List<(ConsoleKey, Func<Task>)>? specialHandlers)
    {
        _textKeyHandlers.Clear();
        if(specialHandlers is not null)
            foreach(var (key, task) in specialHandlers)
            {
                _textKeyHandlers[key] = task;
            }

        foreach(var (key, task) in exitHandlers)
        {
            _textKeyHandlers[key] = async () =>
            {
                await task();
                DisableInputMode();
            };
        }

        _textHandler = textHandler;

        textMode = true;
    }

    private void DisableInputMode()
    {
        textMode = false;
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

    private async Task Handle(CancellationToken token)
    {
        try
        {
            await foreach (var evt in inputChannel.Reader.ReadAllAsync(token))
                if(!textMode)
                    await HandleBind(evt);
                else
                    await HandleInput(evt);
        }
        catch (OperationCanceledException) { }
    }

    private async Task HandleBind(TerminalInput evt)
    {
        var indifferentBindsTask = Task.Run(async () =>
        {
            await ExecuteBindings(
                indifferentBinds,
                ctx => (evt.Key, ctx),
                evt.InputData
            );
        }, CancellationToken.None);

        var sequenceBindsTask = Task.Run(async () =>
        {
            //Reset timeout
            await ResetTimer();

            currentNode.Children.TryGetValue(evt.Key, out var node);

            //Not valid keybind sequence
            if (node is null)
            {
                await sequenceTextChannel.Writer.WriteAsync(string.Empty);
                await ResetSequence();
                return;
            }

            currentNode = node;
            currentSequence.Add(evt.Key);

            await sequenceTextChannel.Writer.WriteAsync(await SequenceText() ?? string.Empty);

            //No binds at this node
            if (!node.IsTerminal)
                return;

            var executedBinds = await ExecuteBindings(
                node.Bindings,
                ctx => ctx,
                evt.InputData
            );

            if(executedBinds)
            {
                await sequenceTextChannel.Writer.WriteAsync(string.Empty);
                await ResetSequence();
            }

        }, CancellationToken.None);
        
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

    private async Task HandleInput(TerminalInput evt)
    {
        if(evt.Key.InputType != Enums.InputType.Keyboard)
            return;

        if(_textKeyHandlers.TryGetValue(evt.Key.KeyboardKey!.Value, out var handler))
        {
            await handler();
            return;
        }
        
        if(_textHandler is not null)
            await _textHandler(evt.InputData.UTFChar!.Value);
    }
}
