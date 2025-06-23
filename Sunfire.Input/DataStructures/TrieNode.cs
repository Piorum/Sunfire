using Sunfire.Input.Types;

namespace Sunfire.Input.DataStructures;

public class TrieNode<TContextEnum> where TContextEnum : struct, Enum
{
    public Dictionary<Key, TrieNode<TContextEnum>> Children { get; } = [];

    public Dictionary<TContextEnum, Bind?> Bindings { get; } = [];

    public bool IsTerminal => Bindings.Count > 0;
}

