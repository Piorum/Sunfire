using Sunfire.FSUtils.Actions;
using Sunfire.FSUtils.Models;

namespace Sunfire.FSUtils.Extensions;

public static class FSDirectoryExtensions
{
    public static async Task CreateFileAsync(this FSDirectory directory, string name)
    {
        var newFilePath = Path.Combine(directory.FullPath, name);
        var writeAction = new WriteAction(newFilePath, []);
        await directory.ActionQueue.WriteAsync(writeAction);
    }

    public static async Task CreateDirectoryAsync(this FSDirectory directory, string name)
    {
        var newDirPath = Path.Combine(directory.FullPath, name);
        var createDirAction = new CreateDirectoryAction(newDirPath);
        await directory.ActionQueue.WriteAsync(createDirAction);
    }
}