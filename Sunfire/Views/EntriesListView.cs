using System.Collections.Concurrent;
using Sunfire.FSUtils;
using Sunfire.FSUtils.Models;
using Moonfire.Logging;
using Sunfire.Registries;
using Sunfire.Views.Text;
using Moonfire.Ansi.Models;

namespace Sunfire.Views;

public class EntriesListView : List
{
    private static readonly ConcurrentDictionary<string, FSEntry> previouslySelectedEntries = [];

    private SortedEntriesCache.LabelSortOptions sortOptions = SortedEntriesCache.LabelSortOptions.None;

    private string currentPath = string.Empty;
    private int selectedIndex = 0;

    private List<EntryLabelView> backLabels = [];
    private List<EntryLabelView> frontLabels = [];    
    
    private CancellationTokenSource? labelsGenCts;

    //Nav
    public async void Nav(int delta)
    {
        if(MaxIndex == -1)
            return; 

        var targetIndex = selectedIndex + delta;
        targetIndex = Math.Clamp(targetIndex, 0, MaxIndex);

        selectedIndex = targetIndex;

        Program.App.Renderer.EnqueueAction(Invalidate);
    }
    public void Nav(FSEntry? entry)
    {
        var index = SortedEntriesCache.GetIndexOfEntry((currentPath, sortOptions), entry);

        if(index is not null && index != selectedIndex)
        {
            selectedIndex = index.Value;
            Program.App.Renderer.EnqueueAction(Invalidate);
        }
    }

    //Tag Helpers
    public (FSEntry? entry, bool enabled) ToggleOrUpdateCurrentEntryTag(AnsiTruecolor color)
    {
        var currentLabel = GetCurrentLabel();
        if(currentLabel is null)
            return (null, false);

        var enabled = TagCache.ToggleOrUpdateTag(currentLabel.Entry, color);
        Program.App.Renderer.EnqueueAction(currentLabel.Invalidate);

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
    public void UpdateCurrentPath(string path, string? overrideSelectedEntry = null)
    {
        currentPath = path;
        selectedIndex = 0;
        UpdateBackLabels(overrideSelectedEntry);
    }
    public void ToggleHidden()
    {
        sortOptions ^= SortedEntriesCache.LabelSortOptions.ShowHidden;
        UpdateBackLabels();
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

    private void UpdateBackLabels(string? overrideSelectedEntry = null)
    {
        var token = SecureLabelsGenToken();

        var path = currentPath;

        List<EntryLabelView> labels;

        if(!Directory.Exists(path))
            labels = [];
        else
            labels = [.. SortedEntriesCache.Get((currentPath, sortOptions))];

        if(overrideSelectedEntry is not null)
        {
            var overrideEntry = labels.FirstOrDefault(e => e.Entry.Name == overrideSelectedEntry);

            if(overrideEntry is not null)
                SaveEntry(path, overrideEntry.Entry);
        }

        int index;
        int? previouslySelectedIndex = null;
            if(previouslySelectedEntries.TryGetValue(currentPath, out var previouslySelectedEntry))
                previouslySelectedIndex = SortedEntriesCache.GetIndexOfEntry((path, sortOptions), previouslySelectedEntry);

        if(previouslySelectedIndex is not null)
            index = previouslySelectedIndex.Value;
        else
            index = 0;

        if(!token.IsCancellationRequested)
        {
            backLabels = labels;
            selectedIndex = index;
            
            Program.App.Renderer.EnqueueAction(Invalidate);
        }
    }

    override protected void OnArrange()
    {
        if(frontLabels != backLabels)
        {
            frontLabels = backLabels;

            Clear();
            AddLabels(frontLabels);
        }

        SelectedIndex = selectedIndex;

        base.OnArrange();
    }

    private class EntryLabelView : LabelSVSlim
    {
        private static readonly AnsiStyleData directoryStyle = new(ForegroundColor: ColorRegistry.DirectoryColor, Properties: AnsiProperty.Bold);
        private static readonly AnsiStyleData fileStyle = new(ForegroundColor: ColorRegistry.FileColor);

        private FSEntry _entry;
        public FSEntry Entry 
        {
            get => _entry;
            set => (_entry, built) = (value, false);
        }

        private int tagCacheVersion = -1;
        
        private bool built = false;

        override protected void OnArrange()
        {
            CheckTag();

            if(built)
                return;

            BuildSegments();
        }

        private void CheckTag()
        {
            //If tag cache version changed, check for tag color (Found color or Null(No Tag)), if color changed rebuild
            if(tagCacheVersion != TagCache.Version)
            {
                AnsiTruecolor? newColor = TagCache.TryGetValue(_entry, out var color)
                    ? color
                    : null;

                if(newColor != tagColor)
                    (tagColor, tagCacheVersion) = (newColor, TagCache.Version);
            }
        }

        private void BuildSegments()
        {
            AnsiStyleData style = Entry.IsDirectory
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
        private static readonly ConcurrentDictionary<FSEntry, AnsiTruecolor> cache = new();
        public static int Version = 0;

        public static bool TryGetValue(FSEntry entry, out AnsiTruecolor color) =>
            cache.TryGetValue(entry, out color);

        public static bool ToggleOrUpdateTag(FSEntry entry, AnsiTruecolor newColor)
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

        public static void TagEntry(FSEntry entry, AnsiTruecolor color)
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
    }

    private static class SortedEntriesCache
    {
        private static readonly ConcurrentDictionary<string, ConcurrentDictionary<(string path, LabelSortOptions options), Lazy<List<EntryLabelView>>>> sortedLabelsCache = [];

        public static List<EntryLabelView> Get((string path, LabelSortOptions options) key)
        {
            var lazy = GetOrAddSortedEntries(key);

            return lazy.Value;
        }

        public static int? GetIndexOfEntry((string path, LabelSortOptions options) key, FSEntry? entry)
        {
            var currentEntries = Get(key);
            var indexOfEntry = currentEntries.FindIndex(e => e.Entry == entry);

            return indexOfEntry < 0
                ? null
                : indexOfEntry;
        }

        private static Lazy<List<EntryLabelView>> GetOrAddSortedEntries((string path, LabelSortOptions options) key)
        {
            var perPath = sortedLabelsCache.GetOrAdd(key.path, _ => new());

            return perPath.GetOrAdd(key, k => new Lazy<List<EntryLabelView>>(() =>
                {
                    Logger.Debug(nameof(Sunfire), $"Sorting Labels for {k.path} | {(int)k.options}");
                    
                    var entries = FSCache.GetEntries(k.path);
                    var sortedEntries = SortEntries(entries, k.options);
                    var sortedLabels = sortedEntries.Select(e => new EntryLabelView() { Entry = e });

                    return [.. sortedLabels];
                }));
        }

        private static IOrderedEnumerable<FSEntry> SortEntries(IEnumerable<FSEntry> entries, LabelSortOptions options)
        {
            if(!options.HasFlag(LabelSortOptions.ShowHidden))
                entries = entries.Where(e => !e.Attributes.HasFlag(FileAttributes.Hidden));

            return entries
                .OrderByDescending(e => e.IsDirectory)
                .ThenByDescending(e => e.Attributes.HasFlag(FileAttributes.Hidden))
                .ThenBy(e => e.Name, StringComparer.OrdinalIgnoreCase);
        }

        public static void Clear(string directory) =>
            sortedLabelsCache.TryRemove(directory, out _);

        public static void Clear() =>
            sortedLabelsCache.Clear();

        [Flags]
        public enum LabelSortOptions
        {
            None = 0,
            ShowHidden = 1,
        }
    }
}
