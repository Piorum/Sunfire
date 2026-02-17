using System.Collections.Concurrent;
using System.IO.Enumeration;
using Sunfire.FSUtils.Models;
using Sunfire.Logging;

namespace Sunfire.FSUtils;

public static class FSCache
{
    private static readonly ConcurrentDictionary<string, Lazy<Task<List<FSEntry>>>> _cache = [];

    public static async Task<List<FSEntry>> GetEntries(string path)
    {
        if(!Directory.Exists(path))
            return [];

        var lazyTask = GetOrAddEntries(path);

        return await lazyTask.Value;
    }

    private static Lazy<Task<List<FSEntry>>> GetOrAddEntries(string path) =>
        _cache.GetOrAdd(path, k => new Lazy<Task<List<FSEntry>>>(Task.Run(async () =>
            {
                await Logger.Debug(nameof(FSUtils), $"Getting \"{k}\" Entries");

                var enumerator = new FileSystemEnumerable<FSEntry>(
                    k,
                    (ref entry) => new FSEntry(ref entry, k),
                    new EnumerationOptions 
                    { 
                        IgnoreInaccessible = true,
                        AttributesToSkip = 0
                    }
                );
                
                List<FSEntry> tmp = [];

                foreach(var entry in enumerator)
                {
                    tmp.Add(entry);
                };

                return tmp;
            }))
        );

    public static void Invalidate(string path) =>
        _cache.Remove(path, out var _);

    public static void Clear() =>
        _cache.Clear();
}
