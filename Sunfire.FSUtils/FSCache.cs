using System.Collections.Concurrent;
using System.IO.Enumeration;
using Sunfire.FSUtils.Models;

namespace Sunfire.FSUtils;

public class FSCache
{
    private readonly Dictionary<string, List<FSEntry>> _cache = [];

    public List<FSEntry> GetEntries(string path)
    {
        if(_cache.TryGetValue(path, out var entries))
            return entries;

        if(!Directory.Exists(path))
            return [];

        var enumerator = new FileSystemEnumerable<FSEntry>(
            path,
            (ref FileSystemEntry entry) => new FSEntry(ref entry, path),
            new EnumerationOptions { IgnoreInaccessible = true }
        );

        entries = [.. enumerator];

        _cache.Add(path, entries);

        return entries;
    }

    public void Invalidate(string path)
    {
        _cache.Remove(path, out var _);
    }
}
