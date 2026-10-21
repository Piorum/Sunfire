using Sunfire.FSUtils.Models;
using Moonfire.Rendering.Interfaces;
using Sunfire.Views;

namespace Sunfire.Previewers;

public class DirectoryPreviewer : PreviewView.IPreviewer
{
    private readonly EntriesListView previewEntriesList = new();

    public IRelativeMoonfireView? Update(FSEntry entry)
    {
        previewEntriesList.UpdateCurrentPath(entry.Path);

        return previewEntriesList;
    }

    public void CleanUp(){}

    public void ToggleHidden() =>
        previewEntriesList.ToggleHidden();
}
