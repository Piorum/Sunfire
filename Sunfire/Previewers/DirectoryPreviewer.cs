using Sunfire.FSUtils.Models;
using Moonfire.Rendering.Interfaces;
using Sunfire.Views;

namespace Sunfire.Previewers;

public class DirectoryPreviewer : PreviewView.IPreviewer
{
    private readonly EntriesListView previewEntriesList = new();

    public async Task<IRelativeMoonfireView?> Update(FSEntry entry)
    {
        await previewEntriesList.UpdateCurrentPath(entry.Path);

        return previewEntriesList;
    }

    public Task CleanUp() => Task.CompletedTask;

    public async Task ToggleHidden() =>
        await previewEntriesList.ToggleHidden();
}
