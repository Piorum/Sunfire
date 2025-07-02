using Sunfire.FSUtils.Actions;

namespace Sunfire.FSUtils.Models;

public class FSDirectory : FSEntry
{
    public int ContentCount { get; internal set; }

    private readonly FSService _fsService;

    internal FSDirectory(FSService fsService)
    {
        _fsService = fsService;
    }

    public async Task<IEnumerable<FSEntry>> GetChildrenAsync(bool forceRefresh = false)
    {
        return await _fsService.LoadChildrenAsync(FullPath, forceRefresh);
    }

    public async Task CreateFileAsync(string name)
    {
        var newFilePath = Path.Combine(FullPath, name);
        var writeAction = new WriteAction(newFilePath, []);
        await ActionQueue.WriteAsync(writeAction);
    }

    public async Task CreateDirectoryAsync(string name)
    {
        var newDirPath = Path.Combine(FullPath, name);
        var createDirAction = new CreateDirectoryAction(newDirPath);
        await ActionQueue.WriteAsync(createDirAction);
    }
}
