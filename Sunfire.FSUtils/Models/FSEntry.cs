using Sunfire.FSUtils.Enums;

namespace Sunfire.FSUtils.Models;

public readonly record struct FSEntry
{
    required public readonly string Name { get; init; }
    required public readonly string Path { get; init; }
    required public readonly FSFileType Type { get; init; }
    required public readonly FSFileAttributes Attributes { get; init; }
}
