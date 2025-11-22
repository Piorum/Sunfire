using System.IO.Enumeration;
using Sunfire.FSUtils.Enums;

namespace Sunfire.FSUtils.Models;

public readonly struct FSEntry
{
    public readonly string Name { get; init; }
    public readonly string Directory { get; init; }

    public readonly FSFileType Type { get; init; }
    public readonly FSFileAttributes Attributes { get; init; }

    public string Path => System.IO.Path.Combine(Directory, Name);

    public FSEntry(ref FileSystemEntry entry, string directory)
    {
        Name = entry.FileName.ToString();
        Directory = directory;

        Type = entry.IsDirectory
            ? FSFileType.Directory
            : FSFileType.File;

        Attributes = entry.Attributes.HasFlag(FileAttributes.Hidden)
            ? FSFileAttributes.Hidden
            : FSFileAttributes.None;
    }
}
