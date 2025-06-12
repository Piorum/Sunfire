using SunfireInputParser.DataStructures;
using SunfireInputParser.Types;

namespace SunfireInputParser.Builders;

public class KeybindBuilder<TContextEnum> where TContextEnum : struct, Enum
{
    private Key? firstKey;
    private TrieNode<TContextEnum>? firstNode;
    private TrieNode<TContextEnum>? lastNode;

    private Func<InputData, Task>? binding;
    private List<TContextEnum>? context;

    private bool sequenceIndifferent = false;

    private bool validated = false;

    private static string _noBindingError = "Must provide binding.";
    private static string _noKeybindingError = "Atleast one keybind must be set.";
    private static string _noContextError = "Context must be set for bind.";
    private static string _indifferentWithSequenceError = "Indfferent binds cannot contain more than one node.";

    public KeybindBuilder<TContextEnum> WithSequence(Key key)
    {
        if (firstKey is null)
        {
            firstKey = key;
            firstNode = new();
            lastNode = firstNode;
        }
        else
        {
            TrieNode<TContextEnum> newNode = new();
            lastNode!.Children.Add(key, newNode);
            lastNode = newNode;
        }
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
        if (firstKey is null)
            return Task.FromResult<(string?, bool)>((_noKeybindingError, validated));
        if (context is null)
            return Task.FromResult<(string?, bool)>((_noContextError, validated));

        if (sequenceIndifferent && firstNode != lastNode)
            return Task.FromResult<(string?, bool)>((_indifferentWithSequenceError, validated));

        validated = true;
        return Task.FromResult<(string?, bool)>((null, validated));
    }

    public Task RegisterBind(InputHandler<TContextEnum> inputHandler)
    {
        if (!validated)
        {
            if (binding is null)
                throw new InvalidOperationException(_noBindingError);
            if (firstKey is null)
                throw new InvalidOperationException(_noKeybindingError);
            if (context is null)
                throw new InvalidOperationException(_noContextError);

            if (sequenceIndifferent && firstNode != lastNode)
                throw new InvalidOperationException(_indifferentWithSequenceError);
        }

        if (sequenceIndifferent)
        {
            Bind bind = new(binding!);
            foreach (var ctx in context!)
            {
                inputHandler.indifferentBinds.Add(((Key)firstKey!, ctx), bind);
            }
        }
        else
        {
            Bind bind = new(binding!);
            foreach (var ctx in context!)
                    lastNode!.Bindings.Add(ctx, bind);

            inputHandler.sequenceBindsRoot.Children.Add((Key)firstKey!, firstNode!);
        }

        return Task.CompletedTask;
    }

}
