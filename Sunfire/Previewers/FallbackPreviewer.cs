using Sunfire.FSUtils.Models;
using Sunfire.Tui.Interfaces;
using Sunfire.Views;

namespace Sunfire.Previewers;

public class FallbackPreviewer : PreviewView.IPreviewer
{

    public Task<IRelativeSunfireView?> Update(FSEntry entry, CancellationToken token)
    {
        return Task.FromResult<IRelativeSunfireView?>(null);
    }
}
