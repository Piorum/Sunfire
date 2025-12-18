using Sunfire.FSUtils.Models;
using Sunfire.Tui.Interfaces;
using Sunfire.Views;

namespace Sunfire.Previewers;

public class DirectoryPreviewer : PreviewView.IPreviewer
{
    private readonly EntriesListView previewEntriesList = new();

    public async Task<IRelativeSunfireView?> Update(FSEntry entry)
    {
        await previewEntriesList.UpdateCurrentPath(entry.Path);

        return previewEntriesList;
    }

    public Task CleanUp() => Task.CompletedTask;

    public async Task ToggleHidden() =>
        await previewEntriesList.ToggleHidden();
}
