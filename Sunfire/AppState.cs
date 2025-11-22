using System.Diagnostics;
using Sunfire.Registries;
using Sunfire.Views;
using Sunfire.Views.Text;
using Sunfire.FSUtils;
using Sunfire.FSUtils.Enums;
using Sunfire.FSUtils.Models;
using Sunfire.Logging;
using Sunfire.Tui.Interfaces;

namespace Sunfire;

public static class AppState
{
    private static readonly FSCache fsCache = new();
    private static readonly Dictionary<string, FSEntry?> selectedEntryCache = [];

    private static string currentPath = "";

    private static bool showHidden = false;

    public static async Task ToggleHidden()
    {
        showHidden = !showHidden;
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

            var entries = await fsCache.GetEntries(containerPath);

            var entry = entries.FirstOrDefault(e => e.Name == entryToSelectName);

            selectedEntryCache[containerPath] = entry;

            curDir = curDir.Parent;
        }

        //Populating Views && currentPath
        await Refresh(basePath);
        selectedEntryCache[basePath] = await GetSelectedEntry();
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

        var selectedEntry = await GetSelectedEntry();
        var preview = await GetPreview(selectedEntry);

        await Program.Renderer.EnqueueAction(async () =>
        {

            await RefreshPreview(preview);
            await RefreshDirectoryHint();
            await RefreshSelectionInfo(selectedEntry);
        });
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
            
            switch(selectedEntry.Value.Type)
            {
                case FSFileType.Directory:
                    SVRegistry.BottomLeftLabel.Text = $" Directory {(await fsCache.GetEntries(selectedEntry.Value.Path)).Count}";
                    break;
                case FSFileType.File:
                    break;
            }
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

    private static async Task<(List<LabelSVSlim> newLabels, int previousSelectedIndex)> GetLabelsAndIndex(string path)
    {
        var entries = await GetEntries(path);

        var newLabels = new List<LabelSVSlim>(entries.Count);
        foreach (var entry in entries)
        {
            LabelSVSlim label = new() { Text = entry.Name };
            if(entry.Type == FSFileType.Directory)
                label.TextProperties |= Ansi.Models.SAnsiProperty.Bold;
            
            newLabels.Add(label);
        }

        var previousSelectedIndex = GetPreviousIndex(entries, path);

        return (newLabels, previousSelectedIndex);
    }

    private static async Task<List<FSEntry>> GetEntries(string path) =>
        [.. OrderAndFilterEntries(await fsCache.GetEntries(path))];

    private static IOrderedEnumerable<FSEntry> OrderAndFilterEntries(IEnumerable<FSEntry> entries)
    {
        if(!showHidden)
            entries = entries.Where(e => !e.Attributes.HasFlag(FSFileAttributes.Hidden));

        return entries
            .OrderByDescending(e => e.Type == FSFileType.Directory)
            .ThenByDescending(e => e.Attributes.HasFlag(FSFileAttributes.Hidden))
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

    private static async Task<IRelativeSunfireView?> GetPreview(FSEntry? selectedEntry)
    {
        IRelativeSunfireView? view = null;
        if(selectedEntry is not null)
            switch(selectedEntry.Value.Type)
            {
                case FSFileType.Directory:
                    ListSV previewList = new();

                    await UpdateList(previewList, await GetLabelsAndIndex(selectedEntry.Value.Path));

                    view = previewList;
                    break;
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
        await(await GetSelectedEntry() is var selectedEntry && selectedEntry is not null && selectedEntry.Value.Type == FSFileType.Directory 
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

        var selectedEntry = await GetSelectedEntry();
        var preview = await GetPreview(selectedEntry);

        await Program.Renderer.EnqueueAction(async () =>
        {
            await RefreshSelectionInfo(selectedEntry);
            await RefreshPreview(preview);
        });
    }

    public static async Task HandleFile()
    {
        if(await GetSelectedEntry() is var selectedEntry && selectedEntry is not null)
            switch (selectedEntry.Value.Type)
            {
                case FSFileType.File:
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
                    break;
            }
    }
}
