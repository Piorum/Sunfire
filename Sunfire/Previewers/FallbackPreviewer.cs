using Sunfire.FSUtils.Models;
using Sunfire.Views;

namespace Sunfire.Previewers;

public class FallbackPreviewer : PreviewView.IPreviewer
{
    public Task Update(FSEntry entry)
    {
        throw new NotImplementedException();
    }
}
