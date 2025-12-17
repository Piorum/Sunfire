using System.Collections.Concurrent;
using Sunfire.Ansi.Models;
using Sunfire.FSUtils;
using Sunfire.FSUtils.Models;
using Sunfire.Logging;
using Sunfire.Registries;
using Sunfire.Views.Text;

namespace Sunfire.Views;

public class EntriesListView : ListSV
{
    private static readonly LabelsCache labelsCache = new();

    private EntriesListViewOptions options = EntriesListViewOptions.None;

    private string currentPath = string.Empty;
    private int selectedIndex = 0;

    private List<LabelSVSlim> backLabels = [];
    private List<LabelSVSlim> frontLabels = [];    
    
    private CancellationTokenSource? labelsGenCts;

    public async Task Nav(int delta)
    {
        if(MaxIndex == -1)
            return; 

        var targetIndex = selectedIndex + delta;
        targetIndex = Math.Clamp(targetIndex, 0, MaxIndex);

        if(targetIndex == selectedIndex)
            return;

        selectedIndex = targetIndex;

        await Program.Renderer.EnqueueAction(Invalidate);
    }

    public async Task UpdateCurrentPath(string path)
    {
        if(path == currentPath)
            return;

        currentPath = path;
        await UpdateBackLabels();
    }

    public async Task ToggleHidden()
    {
        options ^= EntriesListViewOptions.ShowHidden;
        await UpdateBackLabels();
    }

    public async Task<FSEntry?> GetCurrentEntry() =>
        await labelsCache.GetCurrentEntry(this);

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

        var path = currentPath;
        if(!Directory.Exists(path))
            labels = [];
        else
            labels = await labelsCache.GetAsync(this);

        if(!token.IsCancellationRequested)
        {
            selectedIndex = 0;
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
        }

        SelectedIndex = selectedIndex;

        await base.OnArrange();
    }

    private class LabelsCache
    {
        private static readonly FSCache fsCache = new();

        private readonly ConcurrentDictionary<(string path, EntriesListViewOptions sortOptions), Lazy<Task<List<FSEntry>>>> sortedEntriesCache = [];
        private readonly ConcurrentDictionary<(string path, EntriesListViewOptions sortOptions), Lazy<Task<List<LabelSVSlim>>>> labelsCache = [];

        public async Task<List<LabelSVSlim>> GetAsync(EntriesListView view)
        {
            var key = (view.currentPath, view.options);

            var sortedEntriesLazy = GetOrAddSortedEntries(key);

            var labelsLazy = GetOrAddLabels(key, sortedEntriesLazy.Value);

            return await labelsLazy.Value;
        }

        public async Task<FSEntry?> GetCurrentEntry(EntriesListView view)
        {
            var index = view.selectedIndex;
            if(index < 0)
                return null;

            var key = (view.currentPath, view.options);

            if(sortedEntriesCache.TryGetValue(key, out var currentEntriesLazy))
            {
                var currentEntries = await currentEntriesLazy.Value;
                if(index < currentEntries.Count)
                {
                    var currentEntry = currentEntries[index];

                    await Logger.Debug(nameof(Sunfire), $"Current Entry \"{currentEntry.Path}\"");

                    return currentEntry;
                }
            }

            return null;
        }

        private Lazy<Task<List<FSEntry>>> GetOrAddSortedEntries((string path, EntriesListViewOptions options) key) =>
            sortedEntriesCache.GetOrAdd(key, k => new Lazy<Task<List<FSEntry>>>(async () =>
                {
                    var entries = await fsCache.GetEntries(k.path, CancellationToken.None);

                    return [.. SortEntries(entries, k.sortOptions)];
                }));

        private Lazy<Task<List<LabelSVSlim>>> GetOrAddLabels((string path, EntriesListViewOptions options) key, Task<List<FSEntry>> sortedEntriesTask) =>
            labelsCache.GetOrAdd(key, k => new Lazy<Task<List<LabelSVSlim>>>(async () =>
                {
                    var entries = await sortedEntriesTask;

                    var list = new List<LabelSVSlim>(entries.Count);
                    foreach(var entry in entries)
                    {
                        list.Add(BuildLabel(entry));
                    }

                    return list;
                }));
        
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

        private static IOrderedEnumerable<FSEntry> SortEntries(IEnumerable<FSEntry> entries, EntriesListViewOptions options)
        {
            if(!options.HasFlag(EntriesListViewOptions.ShowHidden))
                entries = entries.Where(e => !e.Attributes.HasFlag(FileAttributes.Hidden));

            return entries
                .OrderByDescending(e => e.IsDirectory)
                .ThenByDescending(e => e.Attributes.HasFlag(FileAttributes.Hidden))
                .ThenBy(e => e.Name.ToLowerInvariant());
        }
    }

    [Flags]
    public enum EntriesListViewOptions
    {
        None = 0,
        ShowHidden = 1
    }
}
