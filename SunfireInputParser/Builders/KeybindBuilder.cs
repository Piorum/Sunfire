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

    private bool validated = false;

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

    public Task<(string? error, bool valid)> Validate()
    {
        if (binding is null)
            return Task.FromResult<(string?, bool)>(("Must provide binding.", false));
        if (firstKey is null)
            return Task.FromResult<(string?, bool)>(("Atleast one bind must be set.", false));
        if (context is null)
            return Task.FromResult<(string?, bool)>(("Context must be set for bind.", false));

        validated = true;
        return Task.FromResult<(string?, bool)>((null, true));
    }

    public Task RegisterBind(InputHandler<TContextEnum> inputHandler)
    {
        if (!validated)
        {
            if (binding is null)
                throw new InvalidOperationException("Must provide binding.");
            if (firstKey is null)
                throw new InvalidOperationException("Atleast one bind must be set.");
            if (context is null)
                throw new InvalidOperationException("Context must be set for bind.");
        }

        foreach (var ctx in context!)
            lastNode!.Bindings.Add(ctx, binding!);

        inputHandler.bindingRootNode.Children.Add((Key)firstKey!, firstNode!);

        return Task.CompletedTask;
    }

}
