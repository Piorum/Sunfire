using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using Sunfire.Ansi.Models;
using Sunfire.FSUtils;
using Sunfire.FSUtils.Models;
using Sunfire.Registries;
using Sunfire.Views.Text;

namespace Sunfire.Views;

public class EntriesListView : ListSV
{
    private static readonly ConcurrentDictionary<string, FSEntry> previouslySelectedEntries = [];

    private SortedEntriesCache.LabelSortOptions sortOptions = SortedEntriesCache.LabelSortOptions.None;

    private string currentPath = string.Empty;
    private int selectedIndex = 0;

    private List<EntryLabelView> backLabels = [];
    private List<EntryLabelView> frontLabels = [];    
    
    private CancellationTokenSource? labelsGenCts;

    //Nav
    public async Task Nav(int delta)
    {
        if(MaxIndex == -1)
            return; 

        var targetIndex = selectedIndex + delta;
        targetIndex = Math.Clamp(targetIndex, 0, MaxIndex);

        selectedIndex = targetIndex;

        await Program.Renderer.EnqueueAction(Invalidate);
    }
    public async Task Nav(FSEntry? entry)
    {
        var index = await SortedEntriesCache.GetIndexOfEntry((currentPath, sortOptions), entry);

        if(index is not null && index != selectedIndex)
        {
            selectedIndex = index.Value;
            await Program.Renderer.EnqueueAction(Invalidate);
        }
    }

    //Tag Helpers
    public async Task<(FSEntry? entry, bool enabled)> ToggleOrUpdateCurrentEntryTag(SColor color)
    {
        var currentLabel = GetCurrentLabel();
        if(currentLabel is null)
            return (null, false);

        var enabled = TagCache.ToggleOrUpdateTag(currentLabel.Entry, color);
        await Program.Renderer.EnqueueAction(currentLabel.Invalidate);

        return (currentLabel.Entry, enabled);
    }
    public static void ClearTags() =>
        TagCache.Clear();

    //Caching Selected Entry
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

    //Update Helpers
    public async Task UpdateCurrentPath(string path, string? overrideSelectedEntry = null)
    {
        currentPath = path;
        selectedIndex = 0;
        await UpdateBackLabels(overrideSelectedEntry);
    }
    public async Task ToggleHidden()
    {
        sortOptions ^= SortedEntriesCache.LabelSortOptions.ShowHidden;
        await UpdateBackLabels();
    }

    //Helpers
    public FSEntry? GetCurrentEntry() =>
        selectedIndex < backLabels.Count && selectedIndex >= 0 ? backLabels[selectedIndex].Entry : null;
    private EntryLabelView? GetCurrentLabel() =>
        selectedIndex < backLabels.Count && selectedIndex >= 0 ? backLabels[selectedIndex] : null;
    public static void ClearCache(string directory) =>
        SortedEntriesCache.Clear(directory);
    public static void ClearCache() =>
        SortedEntriesCache.Clear();

    private CancellationToken SecureLabelsGenToken()
    {
        labelsGenCts?.Cancel();
        labelsGenCts?.Dispose();
        
        labelsGenCts = new();
        return labelsGenCts.Token;
    }

    private async Task UpdateBackLabels(string? overrideSelectedEntry = null)
    {
        var token = SecureLabelsGenToken();

        IEnumerable<FSEntry> entries;

        var path = currentPath;
        if(!Directory.Exists(path))
            entries = [];
        else
            entries = await SortedEntriesCache.GetAsync((currentPath, sortOptions));

        if(overrideSelectedEntry is not null)
        {
            var overrideEntry = entries.Where(e => e.Name == overrideSelectedEntry);

            if(overrideEntry.Any())
                SaveEntry(path, overrideEntry.First());
        }

        int index;
        int? previouslySelectedIndex = null;
            if(previouslySelectedEntries.TryGetValue(currentPath, out var previouslySelectedEntry))
                previouslySelectedIndex = await SortedEntriesCache.GetIndexOfEntry((path, sortOptions), previouslySelectedEntry);

        if(previouslySelectedIndex is not null)
            index = previouslySelectedIndex.Value;
        else
            index = 0;

        var labels = entries.Select(e => new EntryLabelView() { Entry = e }).ToList();

        if(!token.IsCancellationRequested)
        {
            backLabels = labels;
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

    private class EntryLabelView : LabelSVSlim
    {
        private static readonly StyleData directoryStyle = new(ForegroundColor: ColorRegistry.DirectoryColor, Properties: SAnsiProperty.Bold);
        private static readonly StyleData fileStyle = new(ForegroundColor: ColorRegistry.FileColor);

        private FSEntry _entry;
        public FSEntry Entry 
        {
            get => _entry;
            set => (_entry, built) = (value, false);
        }

        private int tagCacheVersion = -1;
        
        private bool built = false;

        override protected Task OnArrange()
        {
            CheckTag();

            if(built)
                return Task.CompletedTask;

            BuildSegments();

            return Task.CompletedTask;
        }

        private void CheckTag()
        {
            //If tag cache version changed, check for tag color (Found color or Null(No Tag)), if color changed rebuild
            if(tagCacheVersion != TagCache.Version)
            {
                SColor? newColor = TagCache.TryGetValue(_entry, out var color)
                    ? color
                    : null;

                if(newColor != tagColor)
                    (tagColor, tagCacheVersion) = (newColor, TagCache.Version);
            }
        }

        private void BuildSegments()
        {
            StyleData style = Entry.IsDirectory
                ? directoryStyle
                : fileStyle;

            (var icon, var iconColor) = IconRegistry.GetIcon(Entry);

            var segments = new LabelSegment[2]
            {
                new() { Text = $" {icon}", Style = new( ForegroundColor: iconColor ) },
                new() { Text = $"{Entry.Name}", Style = style }
            };

            (Segments, built, Dirty) = (segments, true, true);
        }
    }

    private static class TagCache
    {
        private static readonly ConcurrentDictionary<FSEntry, SColor> cache = new(FSEntryTagComparer.Default);
        public static int Version = 0;

        public static bool TryGetValue(FSEntry entry, out SColor color) =>
            cache.TryGetValue(entry, out color);

        public static bool ToggleOrUpdateTag(FSEntry entry, SColor newColor)
        {
            if(cache.TryGetValue(entry, out var oldColor))
                if(oldColor == newColor)
                {
                    UnTagEntry(entry);
                    return false;
                }

            TagEntry(entry, newColor);

            return true;
        }

        public static void TagEntry(FSEntry entry, SColor color)
        {
            cache[entry] = color;
            Interlocked.Increment(ref Version);
        }

        public static void UnTagEntry(FSEntry entry)
        {
            cache.TryRemove(entry, out _);
            Interlocked.Increment(ref Version);
        }

        public static void Clear()
        {
            cache.Clear();
            Interlocked.Increment(ref Version);
        }
        
        private class FSEntryTagComparer : IEqualityComparer<FSEntry>
        {
            public static readonly FSEntryTagComparer Default = new();

            public bool Equals(FSEntry x, FSEntry y) =>
                string.Equals(x.Name, y.Name) && string.Equals(x.Directory, y.Directory);

            public int GetHashCode([DisallowNull] FSEntry obj) =>
                HashCode.Combine(obj.Name.GetHashCode(StringComparison.Ordinal), obj.Directory.GetHashCode(StringComparison.Ordinal));

        }
    }

    private static class SortedEntriesCache
    {
        private static readonly ConcurrentDictionary<(string path, LabelSortOptions sortOptions), Lazy<Task<List<FSEntry>>>> sortedEntriesCache = [];

        public static async Task<List<FSEntry>> GetAsync((string path, LabelSortOptions options) key)
        {
            var sortedEntriesLazy = GetOrAddSortedEntries(key);

            return await sortedEntriesLazy.Value;
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

        private static IOrderedEnumerable<FSEntry> SortEntries(IEnumerable<FSEntry> entries, LabelSortOptions options)
        {
            if(!options.HasFlag(LabelSortOptions.ShowHidden))
                entries = entries.Where(e => !e.Attributes.HasFlag(FileAttributes.Hidden));

            return entries
                .OrderByDescending(e => e.IsDirectory)
                .ThenByDescending(e => e.Attributes.HasFlag(FileAttributes.Hidden))
                .ThenBy(e => e.Name.ToLowerInvariant());
        }

        public static void Clear(string directory)
        {
            var sortedEntriesToRemove = sortedEntriesCache.Where(e => e.Key.path == directory);

            foreach(var val in sortedEntriesToRemove)
                sortedEntriesCache.TryRemove(val);
        }

        public static void Clear() =>
            sortedEntriesCache.Clear();

        [Flags]
        public enum LabelSortOptions
        {
            None = 0,
            ShowHidden = 1,
        }
    }
}
