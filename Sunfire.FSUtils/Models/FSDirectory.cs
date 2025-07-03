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

    
}
