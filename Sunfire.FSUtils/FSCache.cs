using System.Collections.Concurrent;
using System.IO.Enumeration;
using Sunfire.FSUtils.Models;
using Sunfire.Logging;

namespace Sunfire.FSUtils;

public static class FSCache
{
    private static ConcurrentDictionary<string, Lazy<Task<List<FSEntry>>>> _cache = [];

    public static async Task<List<FSEntry>> GetEntries(string path)
    {
        if(!Directory.Exists(path))
            return [];

        return await GetOrAddEntries(path).Value;
    }

    private static Lazy<Task<List<FSEntry>>> GetOrAddEntries(string path) =>
        _cache.GetOrAdd(path, k => new Lazy<Task<List<FSEntry>>>(async () =>
            {
                await Logger.Debug(nameof(FSUtils), $"Getting \"{path}\" Entries");

                var entries = await Task.Run(async () =>
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
                        tmp.Add(entry);
                    };

                    return tmp;
                });

                return entries;
            }));

    /*public static void Invalidate(string path)
    {
        _cache.Remove(path, out var _);
    }*/

    public static void Clear() =>
        Interlocked.Exchange(ref _cache, new());
}
