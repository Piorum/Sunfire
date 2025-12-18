using System.Collections.Concurrent;
using Sunfire.FSUtils;
using Sunfire.FSUtils.Models;
using Sunfire.Logging;

namespace Sunfire.Views;

public class EntriesListView : ListSV
{
    private static readonly ConcurrentDictionary<string, FSEntry> previouslySelectedEntries = [];

    private LabelsCache.LabelSortOptions sortOptions = LabelsCache.LabelSortOptions.None;

    private string currentPath = string.Empty;
    private int selectedIndex = 0;

    private List<EntryLabelView> backLabels = [];
    private List<EntryLabelView> frontLabels = [];    
    
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

        if(index is not null && index != selectedIndex)
        {
            selectedIndex = index.Value;
            await Program.Renderer.EnqueueAction(Invalidate);
        }
    }

    public void SaveCurrentEntry()
    {
        var path = currentPath;
        var currentEntry = GetCurrentEntry();
        if(currentEntry is not null)
            previouslySelectedEntries[path] = currentEntry.Value;
    }

    public static void SaveEntry(string path, FSEntry entry)
    {
        previouslySelectedEntries[path] = entry;
    }

    public async Task UpdateCurrentPath(string path)
    {
        currentPath = path;
        selectedIndex = 0;
        await UpdateBackLabels();
    }

    public async Task ToggleHidden()
    {
        sortOptions ^= LabelsCache.LabelSortOptions.ShowHidden;
        await UpdateBackLabels();
    }

    public FSEntry? GetCurrentEntry() =>
        selectedIndex < backLabels.Count && selectedIndex >= 0 ? backLabels[selectedIndex].Entry : null;

    public static void ClearCache() =>
        LabelsCache.Clear();

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

        IEnumerable<EntryLabelView> labels;

        var path = currentPath;
        if(!Directory.Exists(path))
            labels = [];
        else
            labels = await LabelsCache.GetAsync((currentPath, sortOptions));

        await Logger.Debug(nameof(Sunfire), "Pulling Previous Entries");
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
        private static readonly ConcurrentDictionary<(string path, LabelSortOptions sortOptions), Lazy<Task<List<EntryLabelView>>>> labelsCache = [];

        public static async Task<List<EntryLabelView>> GetAsync((string path, LabelSortOptions options) key)
        {
            var sortedEntriesLazy = GetOrAddSortedEntries(key);

            var labelsLazy = GetOrAddLabels(key, sortedEntriesLazy.Value);

            return await labelsLazy.Value;
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

        private static Lazy<Task<List<EntryLabelView>>> GetOrAddLabels((string path, LabelSortOptions options) key, Task<List<FSEntry>> sortedEntriesTask) =>
            labelsCache.GetOrAdd(key, k => new Lazy<Task<List<EntryLabelView>>>(async () =>
                {
                    var entries = await sortedEntriesTask;

                    var list = new List<EntryLabelView>(entries.Count);
                    foreach(var entry in entries)
                    {
                        list.Add(new EntryLabelView() { Entry = entry });
                    }

                    return list;
                }));

        private static IOrderedEnumerable<FSEntry> SortEntries(IEnumerable<FSEntry> entries, LabelSortOptions options)
        {
            if(!options.HasFlag(LabelSortOptions.ShowHidden))
                entries = entries.Where(e => !e.Attributes.HasFlag(FileAttributes.Hidden));

            return entries
                .OrderByDescending(e => e.IsDirectory)
                .ThenByDescending(e => e.Attributes.HasFlag(FileAttributes.Hidden))
                .ThenBy(e => e.Name.ToLowerInvariant());
        }

        public static void Clear()
        {
            sortedEntriesCache.Clear();
            labelsCache.Clear();
        }

        [Flags]
        public enum LabelSortOptions
        {
            None = 0,
            ShowHidden = 1,
        }
    }
}
