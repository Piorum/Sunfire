using System.Collections.Concurrent;
using System.IO.Enumeration;
using Sunfire.FSUtils.Models;
using Sunfire.Logging;

namespace Sunfire.FSUtils;

public static class FSCache
{
    private static ConcurrentDictionary<string, List<FSEntry>> _cache = [];

    public static async Task<List<FSEntry>> GetEntries(string path, CancellationToken token)
    {
        if(_cache.TryGetValue(path, out var entries))
            return entries;

        if(!Directory.Exists(path))
            return [];

        await Logger.Debug(nameof(FSUtils), $"Getting \"{path}\" Entries");

        entries = await Task.Run(() =>
        {
            var enumerator = new FileSystemEnumerable<FSEntry>(
                path,
                (ref entry) => new FSEntry(ref entry, path),
                new EnumerationOptions 
                { 
                    IgnoreInaccessible = true,
                    AttributesToSkip = 0
                }
            );
            
            List<FSEntry> tmp = [];

            foreach(var entry in enumerator)
            {
                token.ThrowIfCancellationRequested();
                tmp.Add(entry);
            }

            return tmp;
        });

        _cache.TryAdd(path, entries);

        return entries;
    }

    /*public static void Invalidate(string path)
    {
        _cache.Remove(path, out var _);
    }*/

    public static void Clear() =>
        Interlocked.Exchange(ref _cache, new());
}
