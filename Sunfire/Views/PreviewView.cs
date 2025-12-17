using System.Collections.Concurrent;
using Sunfire.Enums;
using Sunfire.FSUtils.Models;
using Sunfire.Previewers;
using Sunfire.Registries;

namespace Sunfire.Views;

public class PreviewView
{
    private FSEntry currentEntry;

    private ConcurrentDictionary<MediaType, IPreviewer> previewers = [];
    private DirectoryPreviewer directoryPreviewer = new();
    private FallbackPreviewer fallbackPreviewer = new();

    public async Task UpdateCurrentEntry(FSEntry entry)
    {
        currentEntry = entry;

        IPreviewer previewer;
        if(entry.IsDirectory)
            previewer = directoryPreviewer;
        else
            if(previewers.TryGetValue(MediaRegistry.Scanner.Scan(entry), out var mediaPreviewer))
                previewer = mediaPreviewer;
            else
                previewer = fallbackPreviewer;
        
        await previewer.Update(entry);
    }

    public void AddPreviewer(MediaType mediaType, IPreviewer previewer) =>
        previewers.TryAdd(mediaType, previewer);

    public interface IPreviewer
    {
        public Task Update(FSEntry entry);
    }
}

