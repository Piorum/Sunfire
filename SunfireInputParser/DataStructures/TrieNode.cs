using SunfireInputParser.Types;

namespace SunfireInputParser.DataStructures;

public class TrieNode<TContextEnum> where TContextEnum : struct, Enum
{
    public Dictionary<Key, TrieNode<TContextEnum>> Children { get; } = [];

    public Dictionary<TContextEnum, Func<InputData, Task>> Bindings { get; } = [];

    public bool IsTerminal => Bindings.Count > 0;
}

