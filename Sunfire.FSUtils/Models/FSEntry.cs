using System.IO.Enumeration;

namespace Sunfire.FSUtils.Models;

public readonly struct FSEntry
{
    public readonly string Name { get; init; }
    public readonly string Directory { get; init; }
    public readonly string Extension { get; init; }

    public readonly long Size { get; init; }

    public readonly bool IsDirectory { get; init; }

    public readonly FileAttributes Attributes { get; init; }
    
    public string Path => System.IO.Path.Combine(Directory, Name);

    public bool Equals(FSEntry other) => Name == other.Name && Directory == other.Directory;
    public override bool Equals(object? obj) => obj is FSEntry other && Equals(other);
    public override int GetHashCode() => HashCode.Combine(Name, Directory);
    public static bool operator ==(FSEntry left, FSEntry right) => left.Equals(right);
    public static bool operator !=(FSEntry left, FSEntry right) => !left.Equals(right);


    public FSEntry(ref FileSystemEntry entry, string directory)
    {
        Name = entry.FileName.ToString();
        Directory = directory;
        Extension = System.IO.Path.GetExtension(entry.FileName).ToString();

        Size = entry.Length;

        IsDirectory = entry.IsDirectory;

        Attributes = entry.Attributes;

    }
}
