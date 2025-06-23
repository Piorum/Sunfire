using Sunfire.Input.DataStructures;
using Sunfire.Input.Models;

namespace Sunfire.Input.Builders;

public class KeybindBuilder<TContextEnum> where TContextEnum : struct, Enum
{
    private readonly InputHandler<TContextEnum> inputHandler;

    private readonly List<Key> keySequence = [];

    private Func<InputData, Task>? binding;
    private List<TContextEnum>? context;

    private bool sequenceIndifferent = false;

    private bool validated = false;

    private const string _noBindingError = "Must provide binding.";
    private const string _noKeybindingError = "Atleast one keybind must be set.";
    private const string _noContextError = "Context must be set for bind.";
    private const string _indifferentWithSequenceError = "Indfferent binds cannot contain more than one node.";

    internal KeybindBuilder(InputHandler<TContextEnum> _inputHandler)
    {
        inputHandler = _inputHandler;
    }

    public KeybindBuilder<TContextEnum> WithSequence(Key key)
    {
        keySequence.Add(key);
        return this;
    }

    public KeybindBuilder<TContextEnum> WithBind(Func<InputData, Task> bind)
    {
        binding = bind;
        return this;
    }

    public KeybindBuilder<TContextEnum> WithContext(List<TContextEnum> ctx)
    {
        context = ctx;
        return this;
    }

    public KeybindBuilder<TContextEnum> AsIndifferent()
    {
        sequenceIndifferent = true;
        return this;
    }

    public Task<(string? error, bool valid)> Validate()
    {
        if (binding is null)
            return Task.FromResult<(string?, bool)>((_noBindingError, validated));
        if (keySequence.Count == 0)
            return Task.FromResult<(string?, bool)>((_noKeybindingError, validated));
        if (context is null)
            return Task.FromResult<(string?, bool)>((_noContextError, validated));

        if (sequenceIndifferent && keySequence.Count != 1)
            return Task.FromResult<(string?, bool)>((_indifferentWithSequenceError, validated));

        validated = true;
        return Task.FromResult<(string?, bool)>((null, validated));
    }

    public Task RegisterBind()
    {
        if (!validated)
        {
            if (binding is null)
                throw new InvalidOperationException(_noBindingError);
            if (keySequence.Count == 0)
                throw new InvalidOperationException(_noKeybindingError);
            if (context is null)
                throw new InvalidOperationException(_noContextError);

            if (sequenceIndifferent && keySequence.Count != 1)
                throw new InvalidOperationException(_indifferentWithSequenceError);
        }

        if (sequenceIndifferent)
        {
            Bind bind = new(binding!);
            foreach (var ctx in context!)
            {
                var key = (keySequence.First(), ctx);
                inputHandler.indifferentBinds[key] = bind;
            }
        }
        else
        {
            var currentNode = inputHandler.sequenceBindsRoot;
            foreach (var key in keySequence)
            {
                if (currentNode.Children.TryGetValue(key, out TrieNode<TContextEnum>? value))
                    currentNode = value;
                else
                    currentNode.Children.Add(key, new());
            }

            Bind bind = new(binding!);
            foreach (var ctx in context!)
                    currentNode.Bindings.Add(ctx, bind);
        }

        return Task.CompletedTask;
    }

}
