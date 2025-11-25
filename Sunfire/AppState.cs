using System.Diagnostics;
using Sunfire.Registries;
using Sunfire.Views;
using Sunfire.Views.Text;
using Sunfire.FSUtils;
using Sunfire.FSUtils.Models;
using Sunfire.Logging;
using Sunfire.Tui.Interfaces;
using System.Collections.Concurrent;
using Sunfire.Ansi.Models;
using System.Text;

namespace Sunfire;

public static class AppState
{
    private static readonly FSCache fsCache = new();
    private static readonly Dictionary<string, FSEntry?> selectedEntryCache = [];
    private static readonly ConcurrentDictionary<string, List<FSEntry>> sortedEntriesCache = [];
    private static readonly ConcurrentDictionary<string, List<LabelSVSlim>> builtLabelsCache = [];

    private static string currentPath = "";
    private static bool showHidden = false;

    private static readonly List<(FSEntry entry, LabelSVSlim label)> taggedEntries = [];

    private static CancellationTokenSource? previewGenCts;

    private static CancellationToken SecurePreviewGenToken()
    {
        previewGenCts?.Cancel();
        previewGenCts?.Dispose();
        
        previewGenCts = new();
        return previewGenCts.Token;
    }

    public static async Task ToggleHidden()
    {
        showHidden = !showHidden;
        ResetCache();

        await Refresh();
        await Logger.Info(nameof(Sunfire), "Toggled Hidden Entries");
    }

    public static async Task Init()
    {

        //Swap for finding directory program is opened in?
        string basePath;
        if(Program.Options.UseUserProfileAsDefault)
            basePath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        else
            basePath = Environment.CurrentDirectory;

        if (!Directory.Exists(basePath)) 
            throw new("CWD is invalid, Not a directory");

        //Prepopulated selectedEntryCache
        var curDir = new DirectoryInfo(basePath);

        while(curDir?.Parent != null)
        {
            string containerPath = curDir.Parent.FullName;
            string entryToSelectName = curDir.Name;

            var entries = await fsCache.GetEntries(containerPath, default);

            var entry = entries.FirstOrDefault(e => e.Name == entryToSelectName);

            selectedEntryCache[containerPath] = entry;

            curDir = curDir.Parent;
        }

        //Populating Views && currentPath
        await Refresh(basePath);
        selectedEntryCache[basePath] = await GetSelectedEntry();
    }

    public static async Task Reload()
    {
        fsCache.Clear();
        ResetCache();

        await Refresh();
    }

    private static void ResetCache()
    {
        sortedEntriesCache.Clear();
        builtLabelsCache.Clear();
    }

    private static async Task<FSEntry?> GetSelectedEntry() => SVRegistry.CurrentList.MaxIndex >= 0 
        ? (await GetEntries(currentPath))[SVRegistry.CurrentList.SelectedIndex] 
        : null;

    private static LabelSVSlim? GetSelectedLabel() => 
        SVRegistry.CurrentList.GetSelected();

    private static async Task Refresh() => 
        await Refresh(currentPath);

    private static async Task Refresh(string path)
    {
        currentPath = path;

        TaskCompletionSource tcs = new();
        await Program.Renderer.EnqueueAction(async () =>
        {

            await RefreshContainerList();
            await RefreshCurrentList();
            tcs.TrySetResult();
        });
        await tcs.Task;

        var previewToken = SecurePreviewGenToken();

        var selectedEntry = await GetSelectedEntry();

        try
        {   
            var preview = await GetPreview(selectedEntry, previewToken);

            if(!previewToken.IsCancellationRequested)
                await Program.Renderer.EnqueueAction(async () =>
                {

                    await RefreshPreview(preview);
                    await RefreshDirectoryHint();
                    await RefreshSelectionInfo(selectedEntry);
                });
            }
        catch (OperationCanceledException) { }
    }

    private static async Task RefreshContainerList() => 
        await (Directory.GetParent(currentPath) is var dirInfo && dirInfo is null 
            ? ClearAndInvalidateList(SVRegistry.ContainerList) 
            : UpdateList(SVRegistry.ContainerList, await GetLabelsAndIndex(dirInfo.FullName)));

    private static async Task RefreshCurrentList() =>
        await UpdateList(SVRegistry.CurrentList, await GetLabelsAndIndex(currentPath));

    private static async Task RefreshPreview(IRelativeSunfireView? view)
    {
        SVRegistry.PreviewPane.SubViews.Clear();

        if(view is not null)
            SVRegistry.PreviewPane.SubViews.Add(view);
                
        await SVRegistry.PreviewPane.Invalidate();

    }

    private static async Task RefreshDirectoryHint()
    {
        SVRegistry.CurrentBorder.TitleLabel ??= new();
        SVRegistry.CurrentBorder.TitleLabel.Segments = [new() { Text = currentPath }];

        await SVRegistry.CurrentBorder.Invalidate();
    }

    private static async Task RefreshSelectionInfo(FSEntry? selectedEntry)
    {
        //Selection Full Path
        //Misc File Info
        if(selectedEntry is not null)
        {
            SVRegistry.BottomLeftBorder.TitleLabel ??= new();
            SVRegistry.BottomLeftBorder.TitleLabel.Segments = [new() { Text = selectedEntry.Value.Path }];
            
            if(selectedEntry.Value.IsDirectory)
                SVRegistry.BottomLeftLabel.Segments = [new() { Text = $" Directory {(await fsCache.GetEntries(selectedEntry.Value.Path, default)).Count}" }];
            else
                SVRegistry.BottomLeftLabel.Segments = [new() { Text = $" File {selectedEntry.Value.Size}B" }];

        }
        else
        {
            SVRegistry.BottomLeftBorder.TitleLabel = null;
            SVRegistry.BottomLeftLabel.Segments = null;
        }

        await SVRegistry.BottomLeftBorder.Invalidate();
    }

    //Refresh Helpers
    private static async Task ClearAndInvalidateList(ListSV list)
    {
        await list.Clear();
        await list.Invalidate();
    }

    private static async Task UpdateList(ListSV list, (List<LabelSVSlim> newLabels, int previousSelectedIndex) newValues)
    {
        await list.Clear();

        await list.AddLabels(newValues.newLabels);
        list.SelectedIndex = newValues.previousSelectedIndex;

        await list.Invalidate();
    }

    private static async Task<(List<LabelSVSlim> newLabels, int previousSelectedIndex)> GetLabelsAndIndex(string path, CancellationToken token = default)
    {
        var entriesStartTime = DateTime.Now;

        var entries = await GetEntries(path, token);

        await Logger.Debug(nameof(Sunfire), $"Get {path} Entries {(DateTime.Now - entriesStartTime).TotalMicroseconds}us");

        var labelsStartTime = DateTime.Now;

        if(!builtLabelsCache.TryGetValue(path, out var labels))
        {
            labels = new List<LabelSVSlim>(entries.Count);

            SStyle directoryStyle = new(ForegroundColor: ColorRegistry.DirectoryColor, Properties: SAnsiProperty.Bold);
            SStyle fileStyle = new(ForegroundColor: ColorRegistry.FileColor);

            HashSet<string>? taggedPaths = taggedEntries.Count > 0 
                ? [.. taggedEntries.Select(e => e.entry.Path)]
                : null;
              
            foreach (var entry in entries)
            {
                (string icon, SStyle iconStyle) iconInfo;
                SStyle style;

                if(entry.IsDirectory)
                {
                    iconInfo = (IconRegistry.DirectoryIcon, directoryStyle);
                    style = directoryStyle;
                }
                else
                {
                    if(!IconRegistry.SpecialIcons.TryGetValue(entry.Name, out iconInfo) && !IconRegistry.Icons.TryGetValue(entry.Extension, out iconInfo))
                        iconInfo = (IconRegistry.FallbackFileIcon, fileStyle);
                    
                    style = fileStyle;
                }

                var segments = new LabelSVSlim.LabelSegment[2]
                {
                    new() { Text = iconInfo.icon, Style = iconInfo.iconStyle },
                    new() { Text = entry.Name, Style = style }
                };

                LabelSVSlim label = new() { Segments = segments };

                if(taggedPaths is not null && taggedPaths.Contains(entry.Path))
                {
                    label.LabelProperties |= Views.Enums.LabelSVProperty.Tagged;
                    taggedEntries.RemoveAll(e => e.entry.Path == entry.Path);
                    taggedEntries.Add((entry, label));
                }
                
                labels.Add(label);
            }

            builtLabelsCache.TryAdd(path, labels);
        }

        await Logger.Debug(nameof(Sunfire), $"Build {path} Labels {(DateTime.Now - labelsStartTime).TotalMicroseconds}us");

        var previousSelectedIndex = GetPreviousIndex(entries, path);

        return (labels, previousSelectedIndex);
    }

    private static async Task<List<FSEntry>> GetEntries(string path, CancellationToken token = default)
    {
        if(sortedEntriesCache.TryGetValue(path, out var cachedEntries))
            return cachedEntries;
        
        List<FSEntry> newEntries = [.. OrderAndFilterEntries(await fsCache.GetEntries(path, token))];
        sortedEntriesCache.TryAdd(path, newEntries);

        return newEntries;
    }

    private static IOrderedEnumerable<FSEntry> OrderAndFilterEntries(IEnumerable<FSEntry> entries)
    {
        if(!showHidden)
            entries = entries.Where(e => !e.Attributes.HasFlag(FileAttributes.Hidden));

        return entries
            .OrderByDescending(e => e.IsDirectory)
            .ThenByDescending(e => e.Attributes.HasFlag(FileAttributes.Hidden))
            .ThenBy(e => e.Name.ToLowerInvariant());
    }

    private static int GetPreviousIndex(List<FSEntry> entries, string path)
    {
        selectedEntryCache.TryGetValue(path, out var previousEntry);

        if(previousEntry is null)
            return 0;

        return entries.IndexOf((FSEntry)previousEntry) is var index && index >= 0 
            ? index 
            : 0; //If not found in entries use 0
    }

    private static async Task<IRelativeSunfireView?> GetPreview(FSEntry? selectedEntry, CancellationToken token)
    {
        IRelativeSunfireView? view = null;
        if(selectedEntry is not null)
            if (selectedEntry.Value.IsDirectory)
            {
                ListSV previewList = new();

                await UpdateList(previewList, await GetLabelsAndIndex(selectedEntry.Value.Path, token));

                view = previewList;
            }

        return view;
    }

    public static async Task NavUp() => await NavList(-1);
    public static async Task NavDown() => await NavList(1);
    public static async Task NavOut() => 
        await(Directory.GetParent(currentPath) is var dirInfo && dirInfo is not null 
            ? Refresh(dirInfo.FullName) 
            : Task.CompletedTask);
    public static async Task NavIn() => 
        await(await GetSelectedEntry() is var selectedEntry && selectedEntry is not null && selectedEntry.Value.IsDirectory 
            ? Refresh(selectedEntry.Value.Path) 
            : HandleFile());

    //Nav Helpers
    public static async Task NavList(int delta)
    {
        if(SVRegistry.CurrentList.MaxIndex == -1)
            return; 

        var targetIndex = SVRegistry.CurrentList.SelectedIndex + delta;
        targetIndex = Math.Clamp(targetIndex, 0, SVRegistry.CurrentList.MaxIndex);

        if(targetIndex == SVRegistry.CurrentList.SelectedIndex)
            return;

        selectedEntryCache[currentPath] = (await GetEntries(currentPath))[targetIndex];

        TaskCompletionSource tcs = new();
        await Program.Renderer.EnqueueAction(async () =>
        {
            SVRegistry.CurrentList.SelectedIndex = targetIndex;
            await SVRegistry.CurrentList.Invalidate();

            tcs.TrySetResult();
        });
        await tcs.Task;        
        
        var previewToken = SecurePreviewGenToken();

        var selectedEntry = await GetSelectedEntry();

        try
        {
            var preview = await GetPreview(selectedEntry, previewToken);

            if(!previewToken.IsCancellationRequested)
                await Program.Renderer.EnqueueAction(async () =>
                {
                    await RefreshSelectionInfo(selectedEntry);
                    await RefreshPreview(preview);
                });
        }
        catch (OperationCanceledException) { }
    }

    public static async Task ToggleTagOnSelectedEntry()
    {
        var currentEntry = await GetSelectedEntry();
        var currentLabel = GetSelectedLabel();

        if(currentEntry is null || currentLabel is null)
            return;

        currentLabel.LabelProperties ^= Views.Enums.LabelSVProperty.Tagged;
        taggedEntries.Add((currentEntry.Value, currentLabel));

        await Logger.Debug(nameof(Sunfire), $"Tagged {currentEntry.Value.Path}");

        if(SVRegistry.CurrentList.MaxIndex != SVRegistry.CurrentList.SelectedIndex)
            await NavDown();
        else
            await Program.Renderer.EnqueueAction(SVRegistry.CurrentList.Invalidate);
    }

    public static async Task Search()
    {
        var channelReader = await Program.InputHandler.EnableInputMode();

        StringBuilder sb = new();
        LabelSVSlim label = SVRegistry.BottomLeftLabel;

        await Program.Renderer.EnqueueAction(() =>
        {
            label.Segments = [new() { Text = $" /_", Style = new() }];
            return Task.CompletedTask;
        });
        
        var currentEntries = await GetEntries(currentPath);
        var initialIndex = SVRegistry.CurrentList.SelectedIndex;

        await foreach(var input in channelReader.ReadAllAsync())
        {
            //Ignore mouse input for search
            if(input.Key.InputType != Input.Enums.InputType.Keyboard)
                continue;

            //Restore orignal selection
            if (input.Key.KeyboardKey == ConsoleKey.Escape)
            {
                var initialIndexOffset = SVRegistry.CurrentList.SelectedIndex - initialIndex;
                if(initialIndexOffset != 0)
                    await NavList(-initialIndexOffset);

                break;
            }
            //Stop getting input and leave new selection
            else if (input.Key.KeyboardKey == ConsoleKey.Enter || input.Key.KeyboardKey == ConsoleKey.Tab)
                break;

            //Handle normal input
            if(input.Key.KeyboardKey == ConsoleKey.Backspace)
            {
                if(sb.Length > 0)
                    sb.Remove(sb.Length - 1, 1);
            }
            else if(input.InputData.UTFChar != default)
            {
                sb.Append(input.InputData.UTFChar);
            }
            
            string search = sb.ToString();
            bool isSearchEmpty = search.Length == 0;

            var bestMatch = GetBestMatch(currentEntries, search);
            bool matchFound = bestMatch is not null;

            if(matchFound)
            {
                var newSelectedIndex = currentEntries.IndexOf(bestMatch!.Value);
                int offset = SVRegistry.CurrentList.SelectedIndex - newSelectedIndex;

                if(offset != 0)
                {
                    await NavList(-offset);
                    await Logger.Debug(nameof(Sunfire), $"Searched \"{search}\" with result \"{bestMatch.Value.Name}\"");
                }
            }

            SColor? searchTextColor = (!matchFound && !isSearchEmpty) ? ColorRegistry.Red : null;

            await Program.Renderer.EnqueueAction(() =>
            {
                label.Segments = [new() { Text = $" /{sb}", Style = new( ForegroundColor: searchTextColor ) }, new() { Text = " ", Style = new( ForegroundColor: searchTextColor, Properties: SAnsiProperty.Underline )}]; 
                return Task.CompletedTask;
            });
        }

        await Program.InputHandler.DisableInputMode();
        await Program.Renderer.EnqueueAction(async () => await RefreshSelectionInfo(await GetSelectedEntry()));
    }

    private static FSEntry? GetBestMatch(List<FSEntry> entries, string search)
    {
        if(string.IsNullOrEmpty(search)) return null;

        return entries
            .Select(entry => new { Entry = entry, Score = ScoreMatch(entry.Name, search)})
            .Where(x => x.Score > 0)
            .OrderByDescending(x => x.Score)
            .ThenBy(x => x.Entry.Name.Length) //Prefer shorter
            .Select(x => (FSEntry?)x.Entry)
            .FirstOrDefault();
    }

    private static int ScoreMatch(string text, string search)
    {
        if (text.Equals(search, StringComparison.OrdinalIgnoreCase)) return 4; //Exact
        if (text.StartsWith(search, StringComparison.OrdinalIgnoreCase)) return 3; //Starts With
        if (text.Contains(search, StringComparison.OrdinalIgnoreCase)) return 2; //Contains
        if (IsFuzzyMatch(text, search)) return 1; //Fuzzy
        
        return 0; //No Match
    }

    private static bool IsFuzzyMatch(string text, string search)
    {
        int searchIndex = 0;
        int searchLength = search.Length;

        foreach(char c in text)
            if (char.ToUpperInvariant(c) == char.ToUpperInvariant(search[searchIndex]))
            {
                searchIndex++;
                if(searchIndex == searchLength) return true;
            }

        return false;
    }

    public static async Task HandleFile()
    {
        if(await GetSelectedEntry() is var selectedEntry && selectedEntry is not null && !selectedEntry.Value.IsDirectory)
            Process.Start(
                new ProcessStartInfo()
                {
                    FileName = "xdg-open",
                    Arguments = selectedEntry.Value.Path,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                }
            );
    }
}
