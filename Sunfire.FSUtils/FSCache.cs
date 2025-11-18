using System.Collections.Concurrent;

namespace Sunfire.FSUtils;

public class FSCache
{
    private readonly ConcurrentDictionary<string, List<FSEntry>> _cache = [];

    public List<FSEntry> GetEntries(string path)
    {
        if(_cache.TryGetValue(path, out var entries))
            return entries;

        entries ??= [];

        var directoryInfo = new DirectoryInfo(path);
        var info = directoryInfo.GetFileSystemInfos()
            .OrderByDescending(e => Directory.Exists(e.FullName))
            .ThenByDescending(e => (e.Attributes & FileAttributes.Hidden) != 0)
            .ThenBy(e => e.Name.ToLowerInvariant());

        foreach (var entry in info)
            entries.Add(
                new FSEntry() 
                { 
                    Name = entry.Name, 
                    Path = entry.FullName, 
                    Type = Directory.Exists(entry.FullName) 
                        ? FSFileType.Directory 
                        : FSFileType.File 
                }
            );

        _cache.TryAdd(path, entries);

        return entries;
    }

    public void Invalidate(string path)
    {
        _cache.TryRemove(path, out var _);
    }
}
