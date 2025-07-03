using Sunfire.FSUtils.Actions;
using Sunfire.FSUtils.Models;

namespace Sunfire.FSUtils.Extensions;

public static class FSEntryExtensions
{
    public static async Task DeleteAsync(this FSEntry entry)
    {
        var deleteAction = new DeleteAction(entry);
        await entry.ActionQueue.WriteAsync(deleteAction);
    }

    public static async Task MoveAsync(this FSEntry entry, string newPath)
    {
        var moveAction = new MoveAction(entry, newPath);
        await entry.ActionQueue.WriteAsync(moveAction);
    }
}
