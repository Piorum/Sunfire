using System.Diagnostics;
using Sunfire.Registries;
using Sunfire.Views;
using Sunfire.Views.Text;
using Sunfire.FSUtils;
using Sunfire.FSUtils.Models;
using Sunfire.Logging;
using Sunfire.Tui.Interfaces;
using System.Collections.Concurrent;

namespace Sunfire;

public static class AppState
{
    private static readonly FSCache fsCache = new();
    private static readonly Dictionary<string, FSEntry?> selectedEntryCache = [];
    private static readonly ConcurrentDictionary<string, List<FSEntry>> sortedEntriesCache = [];
    private static readonly ConcurrentDictionary<string, List<LabelSVSlim>> builtLabelsCache = [];

    private static string currentPath = "";
    private static bool showHidden = false;

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
        sortedEntriesCache.Clear();

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
        ResetCache();

        await Refresh();
    }

    private static void ResetCache()
    {
        fsCache.Clear();
        sortedEntriesCache.Clear();
        builtLabelsCache.Clear();
    }

    private static async Task<FSEntry?> GetSelectedEntry() => SVRegistry.CurrentList.MaxIndex >= 0 
        ? (await GetEntries(currentPath))[SVRegistry.CurrentList.SelectedIndex] 
        : null;

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
        SVRegistry.CurrentBorder.TitleLabel.Text = currentPath;

        await SVRegistry.CurrentBorder.Invalidate();
    }

    private static async Task RefreshSelectionInfo(FSEntry? selectedEntry)
    {
        //Selection Full Path
        //Misc File Info
        if(selectedEntry is not null)
        {
            SVRegistry.BottomLeftBorder.TitleLabel ??= new();
            SVRegistry.BottomLeftBorder.TitleLabel.Text = selectedEntry.Value.Path;
            
            if(selectedEntry.Value.IsDirectory)
                SVRegistry.BottomLeftLabel.Text = $" Directory {(await fsCache.GetEntries(selectedEntry.Value.Path, default)).Count}";
            else
                SVRegistry.BottomLeftLabel.Text = $" File {selectedEntry.Value.Size}B";

        }
        else
        {
            SVRegistry.BottomLeftBorder.TitleLabel = null;
            SVRegistry.BottomLeftLabel.Text = string.Empty;
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
            foreach (var entry in entries)
            {
                char Icon;
                if(entry.IsDirectory)
                    Icon = '';
                else if(!MediaRegistry.SpecialIcons.TryGetValue(entry.Name, out Icon) && !MediaRegistry.Icons.TryGetValue(entry.Extension, out Icon))
                        Icon = '';

                LabelSVSlim label = new() { Text = $"{Icon} {entry.Name}" };
                
                if(entry.IsDirectory)
                    label.TextProperties |= Ansi.Models.SAnsiProperty.Bold;
                
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
