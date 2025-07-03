using Sunfire.FSUtils.Actions;
using Sunfire.FSUtils.Models;

namespace Sunfire.FSUtils.Extensions;

public static class FSFileExtensions
{
    public static async Task<byte[]> ReadBytesAsync(this FSFile file)
    {
        return await File.ReadAllBytesAsync(file.FullPath);
    }

    public static async Task<string> ReadTextAsync(this FSFile file)
    {
        return await File.ReadAllTextAsync(file.FullPath);
    }

    public static async Task WriteBytesAsync(this FSFile file, byte[] content)
    {
        var writeAction = new WriteAction(file.FullPath, content);
        await file.ActionQueue.WriteAsync(writeAction);
    }

    public static async Task WriteTextAsync(this FSFile file, string content)
    {
        var writeAction = new WriteAction(file.FullPath, System.Text.Encoding.UTF8.GetBytes(content));
        await file.ActionQueue.WriteAsync(writeAction);
    }
}