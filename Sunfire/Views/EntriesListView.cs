using System.Collections.Concurrent;
using System.Diagnostics;
using Sunfire.Ansi.Models;
using Sunfire.FSUtils;
using Sunfire.FSUtils.Models;
using Sunfire.Logging;
using Sunfire.Registries;
using Sunfire.Views.Text;

namespace Sunfire.Views;

public class EntriesListView : ListSV
{
    private static readonly FSCache fsCache = new();
    private static readonly LabelsCache labelsCache = new();

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

        IEnumerable<LabelSVSlim> labels;

        if(!Directory.Exists(currentPath))
            labels = [];
        else
        {
            var path = currentPath;

            //Cache even if this update phase gets cancelled
            Stopwatch sw = new();

            sw.Restart();
            var entries = await fsCache.GetEntries(path, CancellationToken.None);
            sw.Stop();
            var getEntriesTime = sw.Elapsed.TotalMicroseconds;

            sw.Restart();
            labels = labelsCache.Get(path, entries);
            sw.Stop();
            var getLabelsTime = sw.Elapsed.TotalMicroseconds;

            await Logger.Debug(nameof(Sunfire), $"Get {path} Entries {getEntriesTime}us");
            await Logger.Debug(nameof(Sunfire), $"Get {path} Labels {getLabelsTime}us");
        }

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

    private class LabelsCache
    {
        private readonly ConcurrentDictionary<string, List<LabelSVSlim>> cache = [];
        public List<LabelSVSlim> Get(string path, IEnumerable<FSEntry> entries)
        {
            if(cache.TryGetValue(path, out var labels))
                return labels;

            labels = [];
            foreach(var entry in entries)
            {
                labels.Add(BuildLabel(entry));
            }

            cache.TryAdd(path, labels);

            return labels;
        }
        
        private static readonly SStyle directoryStyle = new(ForegroundColor: ColorRegistry.DirectoryColor, Properties: SAnsiProperty.Bold);
        private static readonly SStyle fileStyle = new(ForegroundColor: ColorRegistry.FileColor);

        private static LabelSVSlim BuildLabel(FSEntry entry)
        {
            SStyle style;

            if(entry.IsDirectory)
                style = directoryStyle;
            else
                style = fileStyle;

            var segments = new LabelSVSlim.LabelSegment[2]
            {
                new() { Text = " ", Style = style },
                new() { Text = entry.Name, Style = style }
            };

            LabelSVSlim label = new() { Segments = segments };

            return label;
        }
    }
}
