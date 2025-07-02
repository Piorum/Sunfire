using Sunfire.FSUtils.Enums;
using Sunfire.FSUtils.Interfaces;
using Sunfire.FSUtils.Models;

namespace Sunfire.FSUtils.Actions;

public class WriteAction(string filePath, byte[] content) : IModificationAction
{
    public string Description => $"Write {Content.Length} bytes to {FilePath}";
    public FSEntry? Target => null; // This action creates a file, so there's no existing target
    public ActionProperty ActionProperties { get; } = ActionProperty.Destructive;
    
    public string FilePath { get; } = filePath;
    public byte[] Content { get; } = content;

    public async Task ExecuteAsync()
    {
        await File.WriteAllBytesAsync(FilePath, Content);
    }
}
