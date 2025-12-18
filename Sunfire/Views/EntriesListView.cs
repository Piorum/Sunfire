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
    private static readonly ConcurrentDictionary<string, FSEntry> previouslySelectedEntries = [];

    private LabelsCache.LabelSortOptions sortOptions = LabelsCache.LabelSortOptions.None;

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

    public async Task Nav(FSEntry? entry)
    {
        var index = await LabelsCache.GetIndexOfEntry((currentPath, sortOptions), entry);

        if(index is not null)
        {
            selectedIndex = index.Value;
            await Program.Renderer.EnqueueAction(Invalidate);
        }
    }

    public async Task SaveCurrentEntry()
    {
        var currentEntry = await LabelsCache.GetCurrentEntry((currentPath, sortOptions), selectedIndex);
        if(currentEntry is not null)
            previouslySelectedEntries[currentPath] = currentEntry.Value;
    }

    public static void SaveEntry(string path, FSEntry entry)
    {
        previouslySelectedEntries[path] = entry;
    }

    public async Task UpdateCurrentPath(string path)
    {
        if(path == currentPath)
            return;

        currentPath = path;
        selectedIndex = 0;
        await UpdateBackLabels();
    }

    public async Task ToggleHidden()
    {
        sortOptions ^= LabelsCache.LabelSortOptions.ShowHidden;
        await UpdateBackLabels();
    }

    public async Task<FSEntry?> GetCurrentEntry() =>
        await LabelsCache.GetCurrentEntry((currentPath, sortOptions), selectedIndex);

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
            labels = await LabelsCache.GetAsync((currentPath, sortOptions));

        int index;
        int? previouslySelectedIndex = null;
        if(previouslySelectedEntries.TryGetValue(currentPath, out var previouslySelectedEntry))
            previouslySelectedIndex = await LabelsCache.GetIndexOfEntry((path, sortOptions), previouslySelectedEntry);

        if(previouslySelectedIndex is not null)
            index = previouslySelectedIndex.Value;
        else
            index = 0;

        if(!token.IsCancellationRequested)
        {
            backLabels = [.. labels];
            selectedIndex = index;
            
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

    private static class LabelsCache
    {
        private static readonly ConcurrentDictionary<(string path, LabelSortOptions sortOptions), Lazy<Task<List<FSEntry>>>> sortedEntriesCache = [];
        private static readonly ConcurrentDictionary<(string path, LabelSortOptions sortOptions), Lazy<Task<List<LabelSVSlim>>>> labelsCache = [];

        public static async Task<List<LabelSVSlim>> GetAsync((string path, LabelSortOptions options) key)
        {
            var sortedEntriesLazy = GetOrAddSortedEntries(key);

            var labelsLazy = GetOrAddLabels(key, sortedEntriesLazy.Value);

            return await labelsLazy.Value;
        }

        public static async Task<FSEntry?> GetCurrentEntry((string path, LabelSortOptions options) key, int selectedIndex)
        {
            if(selectedIndex < 0)
                return null;

            if(sortedEntriesCache.TryGetValue(key, out var currentEntriesLazy))
            {
                var currentEntries = await currentEntriesLazy.Value;
                if(selectedIndex < currentEntries.Count)
                {
                    var currentEntry = currentEntries[selectedIndex];

                    await Logger.Debug(nameof(Sunfire), $"Current Entry \"{currentEntry.Path}\"");

                    return currentEntry;
                }
            }

            return null;
        }

        public static async Task<int?> GetIndexOfEntry((string path, LabelSortOptions options) key, FSEntry? entry)
        {
            if(entry is null || !sortedEntriesCache.TryGetValue(key, out var currentEntriesLazy))
                return null;

            var currentEntries = await currentEntriesLazy.Value;
            var indexOfEntry = currentEntries.IndexOf(entry.Value);

            if(indexOfEntry < 0)
                return null;

            return indexOfEntry;
        }

        private static Lazy<Task<List<FSEntry>>> GetOrAddSortedEntries((string path, LabelSortOptions options) key) =>
            sortedEntriesCache.GetOrAdd(key, k => new Lazy<Task<List<FSEntry>>>(async () =>
                {
                    var entries = await FSCache.GetEntries(k.path, CancellationToken.None);

                    return [.. SortEntries(entries, k.sortOptions)];
                }));

        private static Lazy<Task<List<LabelSVSlim>>> GetOrAddLabels((string path, LabelSortOptions options) key, Task<List<FSEntry>> sortedEntriesTask) =>
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

        private static Lazy<Task<List<FSEntry>>>? TryGetEntries((string path, LabelSortOptions options) key)
        {
            sortedEntriesCache.TryGetValue(key, out var currentEntriesLazy);

            return currentEntriesLazy;
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

        private static IOrderedEnumerable<FSEntry> SortEntries(IEnumerable<FSEntry> entries, LabelSortOptions options)
        {
            if(!options.HasFlag(LabelSortOptions.ShowHidden))
                entries = entries.Where(e => !e.Attributes.HasFlag(FileAttributes.Hidden));

            return entries
                .OrderByDescending(e => e.IsDirectory)
                .ThenByDescending(e => e.Attributes.HasFlag(FileAttributes.Hidden))
                .ThenBy(e => e.Name.ToLowerInvariant());
        }

        [Flags]
        public enum LabelSortOptions
        {
            None = 0,
            ShowHidden = 1,
        }
    }
}
