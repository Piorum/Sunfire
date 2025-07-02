
using Mono.Unix;
using Sunfire.FSUtils.Interfaces;
using Sunfire.FSUtils.Models;
using System.Collections.Concurrent;
using System.Threading.Channels;

namespace Sunfire.FSUtils
{
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
                    var unixFileInfo = new UnixFileInfo(path);
                    var fileEntry = new FSFile
                    {
                        FullPath = path,
                        Name = fileInfo.Name,
                        Size = fileInfo.Length,
                        IsHidden = (fileInfo.Attributes & FileAttributes.Hidden) != 0,
                        Owner = unixFileInfo.OwnerUser.UserName,
                        DateModified = fileInfo.LastWriteTimeUtc,
                        ActionQueue = _modificationChannel.Writer,
                        Permissions = GetPermissions(unixFileInfo)
                    };
                    _cache[path] = fileEntry;
                    return Task.FromResult<FSEntry?>(fileEntry);
                }
                else if (Directory.Exists(path))
                {
                    var dirInfo = new DirectoryInfo(path);
                    var unixDirInfo = new UnixDirectoryInfo(path);
                    var dirEntry = new FSDirectory(this)
                    {
                        FullPath = path,
                        Name = dirInfo.Name,
                        IsHidden = (dirInfo.Attributes & FileAttributes.Hidden) != 0,
                        Owner = unixDirInfo.OwnerUser.UserName,
                        DateModified = dirInfo.LastWriteTimeUtc,
                        ActionQueue = _modificationChannel.Writer,
                        ContentCount = dirInfo.GetFileSystemInfos().Length,
                        Permissions = GetPermissions(unixDirInfo)
                    };
                    _cache[path] = dirEntry;
                    return Task.FromResult<FSEntry?>(dirEntry);
                }
            }
            catch (Exception)
            {
                // Could log this exception
                return Task.FromResult<FSEntry?>(null);
            }

            return Task.FromResult<FSEntry?>(null);
        }

        internal async Task<IEnumerable<FSEntry>> LoadChildrenAsync(string parentPath, bool forceRefresh)
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
            catch (Exception)
            {
                // Could log this exception
            }
            return children;
        }

        private FSPermissions GetPermissions(UnixFileSystemInfo info)
        {
            return new FSPermissions
            {
                UserRead = (info.FileAccessPermissions & FileAccessPermissions.UserRead) != 0,
                UserWrite = (info.FileAccessPermissions & FileAccessPermissions.UserWrite) != 0,
                UserExecute = (info.FileAccessPermissions & FileAccessPermissions.UserExecute) != 0,
                GroupRead = (info.FileAccessPermissions & FileAccessPermissions.GroupRead) != 0,
                GroupWrite = (info.FileAccessPermissions & FileAccessPermissions.GroupWrite) != 0,
                GroupExecute = (info.FileAccessPermissions & FileAccessPermissions.GroupExecute) != 0,
                OtherRead = (info.FileAccessPermissions & FileAccessPermissions.OtherRead) != 0,
                OtherWrite = (info.FileAccessPermissions & FileAccessPermissions.OtherWrite) != 0,
                OtherExecute = (info.FileAccessPermissions & FileAccessPermissions.OtherExecute) != 0
            };
        }
    }
}
