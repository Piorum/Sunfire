using System.Collections.Concurrent;
using System.IO.Enumeration;
using Sunfire.FSUtils.Models;

namespace Sunfire.FSUtils;

public class FSCache
{
    private readonly ConcurrentDictionary<string, List<FSEntry>> _cache = [];

    public async Task<List<FSEntry>> GetEntries(string path)
    {
        if(_cache.TryGetValue(path, out var entries))
            return entries;

        if(!Directory.Exists(path))
            return [];

        entries = await Task.Run(() =>
        {
            var enumerator = new FileSystemEnumerable<FSEntry>(
                path,
                (ref FileSystemEntry entry) => new FSEntry(ref entry, path),
                new EnumerationOptions 
                { 
                    IgnoreInaccessible = true,
                    AttributesToSkip = 0
                }
            );
            
            return enumerator.ToList();
        });

        _cache.TryAdd(path, entries);

        return entries;
    }

    public void Invalidate(string path)
    {
        _cache.Remove(path, out var _);
    }
}
