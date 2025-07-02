using Sunfire.FSUtils.Interfaces;
using Sunfire.FSUtils.Models;
using Sunfire.Logging;
using System.Collections.Concurrent;
using System.Threading.Channels;

namespace Sunfire.FSUtils;

public class FSService
{
    private readonly ConcurrentDictionary<string, FSEntry> _cache = new();
    private readonly Channel<IModificationAction> _modificationChannel = Channel.CreateUnbounded<IModificationAction>();

    public ChannelReader<IModificationAction> ModificationRequests => _modificationChannel.Reader;

    public Task<FSEntry?> GetEntryAsync(string path, bool forceRefresh = false)
    {
        if (!forceRefresh && _cache.TryGetValue(path, out var entry))
        {
            return Task.FromResult<FSEntry?>(entry);
        }

        return CreateAndCacheEntry(path);
    }

    private Task<FSEntry?> CreateAndCacheEntry(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                var fileInfo = new FileInfo(path);
                var fileEntry = new FSFile
                {
                    FullPath = path,
                    Name = fileInfo.Name,
                    Size = fileInfo.Length,
                    IsHidden = (fileInfo.Attributes & FileAttributes.Hidden) != 0,
                    Owner = GetOwner(),
                    DateModified = fileInfo.LastWriteTimeUtc,
                    ActionQueue = _modificationChannel.Writer,
                    Permissions = GetPermissions()
                };
                _cache[path] = fileEntry;
                return Task.FromResult<FSEntry?>(fileEntry);
            }
            else if (Directory.Exists(path))
            {
                var dirInfo = new DirectoryInfo(path);
                var dirEntry = new FSDirectory(this)
                {
                    FullPath = path,
                    Name = dirInfo.Name,
                    IsHidden = (dirInfo.Attributes & FileAttributes.Hidden) != 0,
                    Owner = GetOwner(),
                    DateModified = dirInfo.LastWriteTimeUtc,
                    ActionQueue = _modificationChannel.Writer,
                    ContentCount = dirInfo.GetFileSystemInfos().Length,
                    Permissions = GetPermissions()
                };
                _cache[path] = dirEntry;
                return Task.FromResult<FSEntry?>(dirEntry);
            }
        }
        catch (Exception ex)
        {
            Task.Run(() => Logger.Error(nameof(FSUtils), $"{ex}"));
            return Task.FromResult<FSEntry?>(null);
        }

        return Task.FromResult<FSEntry?>(null);
    }

    internal async Task<IEnumerable<FSEntry>> LoadChildrenAsync(string parentPath, DirectoryQueryOptions? options, bool forceRefresh)
    {
        var children = new List<FSEntry>();
        try
        {
            var directory = new DirectoryInfo(parentPath);
            foreach (var fsInfo in directory.GetFileSystemInfos())
            {
                var childEntry = await GetEntryAsync(fsInfo.FullName, forceRefresh);
                if (childEntry != null)
                {
                    childEntry.Parent = (FSDirectory)_cache[parentPath];
                    children.Add(childEntry);
                }
            }
        }
        catch (Exception ex)
        {
            await Logger.Error(nameof(FSUtils), $"{ex}");
        }

        IEnumerable<FSEntry> filteredChildren = children;

        // Apply ShowHidden filter
        if (options != null && !options.ShowHidden)
        {
            filteredChildren = filteredChildren.Where(e => !e.IsHidden);
        }

        // Apply SearchPattern filter
        if (options != null && !string.IsNullOrEmpty(options.SearchPattern))
        {
            filteredChildren = filteredChildren.Where(e => e.Name.Contains(options.SearchPattern, StringComparison.OrdinalIgnoreCase));
        }

        // Apply SortOrder, SortBy, and SortDirection
        if (options != null)
        {
            IOrderedEnumerable<FSEntry> orderedChildren;

            // Primary sort based on SortOrder
            switch (options.SortOrder)
            {
                case Enums.SortOrder.DirectoriesFirst:
                    orderedChildren = filteredChildren.OrderByDescending(e => e is FSDirectory);
                    break;
                case Enums.SortOrder.FilesFirst:
                    orderedChildren = filteredChildren.OrderBy(e => e is FSDirectory);
                    break;
                case Enums.SortOrder.Mixed:
                default:
                    // If mixed, start with the SortBy field directly
                    orderedChildren = options.SortDirection == Enums.SortDirection.Ascending
                        ? filteredChildren.OrderBy(e => GetSortValue(e, options.SortBy))
                        : filteredChildren.OrderByDescending(e => GetSortValue(e, options.SortBy));
                    break;
            }

            // Secondary sort based on SortBy (if SortOrder is not Mixed)
            if (options.SortOrder != Enums.SortOrder.Mixed)
            {
                orderedChildren = options.SortDirection == Enums.SortDirection.Ascending
                    ? orderedChildren.ThenBy(e => GetSortValue(e, options.SortBy))
                    : orderedChildren.ThenByDescending(e => GetSortValue(e, options.SortBy));
            }
            
            filteredChildren = orderedChildren;
        }

        return filteredChildren;
    }

    private static object GetSortValue(FSEntry entry, Enums.SortField sortBy)
    {
        return sortBy switch
        {
            Enums.SortField.Name => entry.Name,
            Enums.SortField.DateModified => entry.DateModified,
            Enums.SortField.Size => entry.Size,
            Enums.SortField.Extension => entry.Extension,
            _ => entry.Name, // Default
        };
    }

    private static string GetOwner()
    {
        return string.Empty;
    }

    private static FSPermissions? GetPermissions()
    {
        return null;
    }
}
