using System.Collections.Concurrent;
using Sunfire.Enums;
using Sunfire.FSUtils.Models;
using Sunfire.Previewers;
using Sunfire.Registries;
using Sunfire.Tui.Interfaces;

namespace Sunfire.Views;

public class PreviewView : PaneSV
{
    private readonly ConcurrentDictionary<MediaType, IPreviewer> previewers = [];
    public readonly DirectoryPreviewer directoryPreviewer = new();
    public readonly FallbackPreviewer fallbackPreviewer = new();

    private IRelativeSunfireView? backView = null;

    private CancellationTokenSource? previewGenCts;

    public async Task Update(FSEntry? entry)
    {
        var token = SecurePreviewGenToken();

        if(entry is null)
        {
            backView = null;
            return;
        }

        IPreviewer previewer;
        if(entry.Value.IsDirectory)
            previewer = directoryPreviewer;
        else
            if(previewers.TryGetValue(MediaRegistry.Scanner.Scan(entry.Value), out var mediaPreviewer))
                previewer = mediaPreviewer;
            else
                previewer = fallbackPreviewer;
        try
        {
            var updatedView = await previewer.Update(entry.Value, token);

            if(!token.IsCancellationRequested)
            {
                backView = updatedView;
                await Program.Renderer.EnqueueAction(Invalidate);
            }
        }
        catch (OperationCanceledException){ }
    }

    public void AddPreviewer(MediaType mediaType, IPreviewer previewer) =>
        previewers.TryAdd(mediaType, previewer);

    override protected async Task OnArrange()
    {
        SubViews.Clear();

        if(backView is not null)
            SubViews.Add(backView);

        await base.OnArrange();
    }

    private CancellationToken SecurePreviewGenToken()
    {
        previewGenCts?.Cancel();
        previewGenCts?.Dispose();
        
        previewGenCts = new();
        return previewGenCts.Token;
    }

    public interface IPreviewer
    {
        public Task<IRelativeSunfireView?> Update(FSEntry entry, CancellationToken token);
    }    
}

