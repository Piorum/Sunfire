using Sunfire.Data;
using Sunfire.FSUtils;
using Sunfire.Views.Text;

namespace Sunfire.Views;

public class EntriesListView(FSCache fsCache) : ListSV
{
    private readonly FSCache _fsCache = fsCache;
    private string currentPath = string.Empty;

    private List<LabelSVSlim> backLabels = [];
    private List<LabelSVSlim> frontLabels = [];    
    
    private CancellationTokenSource? labelsGenCts;

    public async Task UpdateCurrentPath(string path)
    {
        if(path == currentPath)
            return;

        currentPath = path;
        await UpdateBackLabels();

        await Program.Renderer.EnqueueAction(Invalidate);
    }

    private CancellationToken SecureLabelsGenToken()
    {
        labelsGenCts?.Cancel();
        labelsGenCts?.Dispose();
        
        labelsGenCts = new();
        return labelsGenCts.Token;
    }

    private async Task UpdateBackLabels()
    {
        var token = SecureLabelsGenToken();
        
        //Cache even if this update phase gets cancelled
        var entries = await _fsCache.GetEntries(currentPath, CancellationToken.None);
        var labels = (await LabelsCache.GetAsync(entries)).Select(c => c.View);

        if(!token.IsCancellationRequested)
        {
            backLabels = [.. labels];
            
            await Program.Renderer.EnqueueAction(Invalidate);
        }
    }

    override protected async Task OnArrange()
    {
        if(frontLabels != backLabels)
        {
            frontLabels = backLabels;

            await Clear();
            await AddLabels(frontLabels);

            //Update selected index properly here later
            SelectedIndex = 0;
        }

        await base.OnArrange();
    }
}
