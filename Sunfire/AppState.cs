using System.Diagnostics;
using Sunfire.Registries;
using Sunfire.Views;
using Sunfire.Views.Text;
using Sunfire.FSUtils;
using Sunfire.FSUtils.Enums;
using Sunfire.FSUtils.Models;
using Sunfire.Logging;

namespace Sunfire;

public static class AppState
{
    private static readonly FSCache fsCache = new();
    private static readonly Dictionary<string, FSEntry?> selectedEntryCache = [];

    private static string currentPath = "";
    private static FSEntry? SelectedEntry => SVRegistry.CurrentList.MaxIndex >= 0 ? GetEntries(currentPath)[SVRegistry.CurrentList.SelectedIndex] : null;

    private static bool showHidden = false;

    public static async Task ToggleHidden()
    {
        showHidden = !showHidden;
        await Program.Renderer.EnqueueAction(Refresh);
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

            var entries = fsCache.GetEntries(containerPath);

            var entry = entries.FirstOrDefault(e => e.Name == entryToSelectName);

            selectedEntryCache[containerPath] = entry;

            curDir = curDir.Parent;
        }

        //Populating Views && currentPath
        await Refresh(basePath);
        selectedEntryCache[basePath] = SelectedEntry;
    }

    private static async Task Refresh() => 
        await Refresh(currentPath);

    private static async Task Refresh(string path)
    {
        currentPath = path;
        
        await RefreshContainerList();
        await RefreshCurrentList();
        await RefreshPreview();
        await RefreshDirectoryHint();
        await RefreshSelectionInfo();
    }

    private static async Task RefreshContainerList() => 
        await (Directory.GetParent(currentPath) is var dirInfo && dirInfo is null 
            ? ClearAndInvalidateList(SVRegistry.ContainerList) 
            : UpdateList(SVRegistry.ContainerList, dirInfo.FullName));

    private static async Task RefreshCurrentList() =>
        await UpdateList(SVRegistry.CurrentList, currentPath);

    private static async Task RefreshPreview()
    {
        SVRegistry.PreviewPane.SubViews.Clear();

        if(SelectedEntry is not null)
            switch(SelectedEntry.Value.Type)
            {
                case FSFileType.Directory:
                    ListSV previewList = new();

                    await UpdateList(previewList, SelectedEntry.Value.Path);

                    SVRegistry.PreviewPane.SubViews.Add(previewList);
                    break;
            }
                
        await SVRegistry.PreviewBorder.Invalidate();

    }

    private static async Task RefreshDirectoryHint()
    {
        SVRegistry.CurrentBorder.TitleLabel ??= new();
        SVRegistry.CurrentBorder.TitleLabel.Text = currentPath;

        await SVRegistry.CurrentBorder.Invalidate();
    }

    private static async Task RefreshSelectionInfo()
    {
        //Selection Full Path
        if(SelectedEntry is not null)
        {
            SVRegistry.BottomLeftBorder.TitleLabel ??= new();
            SVRegistry.BottomLeftBorder.TitleLabel.Text = SelectedEntry.Value.Path;
        }
        else
            SVRegistry.BottomLeftBorder.TitleLabel = null;

        //Misc File Info
        if(SelectedEntry is not null)
            switch(SelectedEntry.Value.Type)
            {
                case FSFileType.Directory:
                    SVRegistry.BottomLeftLabel.Text = $" Directory {Directory.GetFiles(SelectedEntry.Value.Path).Length + Directory.GetDirectories(SelectedEntry.Value.Path).Length}";
                    break;
                case FSFileType.File:
                    SVRegistry.BottomLeftLabel.Text = $" Size: {new FileInfo(SelectedEntry.Value.Path).Length}B";
                    break;
            }
        else
            SVRegistry.BottomLeftLabel.Text = string.Empty;

        await SVRegistry.BottomLeftBorder.Invalidate();
    }

    //Refresh Helpers
    private static async Task ClearAndInvalidateList(ListSV list)
    {
        await list.Clear();
        await list.Invalidate();
    }

    private static async Task UpdateList(ListSV list, string path)
    {
        await list.Clear();

        var entries = GetEntries(path);
        foreach (var entry in entries)
        {
            LabelSVSlim label = new() { Text = entry.Name };
            if(entry.Type == FSFileType.Directory)
                label.TextProperties |= Ansi.Models.SAnsiProperty.Bold;
            
            await list.AddLabel(label);
        }

        list.SelectedIndex = GetPreviousIndex(entries, path);

        await list.Invalidate();
    }

    private static List<FSEntry> GetEntries(string path) =>
        [.. OrderAndFilterEntries(fsCache.GetEntries(path))];

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

    public static async Task NavUp() => await NavList(-1);
    public static async Task NavDown() => await NavList(1);
    public static async Task NavOut() => 
        await(Directory.GetParent(currentPath) is var dirInfo && dirInfo is not null 
            ? Program.Renderer.EnqueueAction(() => Refresh(dirInfo.FullName)) 
            : Task.CompletedTask);
    public static async Task NavIn() => 
        await(SelectedEntry is not null && SelectedEntry.Value.Type == FSFileType.Directory 
            ? Program.Renderer.EnqueueAction(() => Refresh(SelectedEntry.Value.Path)) 
            : HandleFile());

    //Nav Helpers
    public static async Task NavList(int delta)
    {
        var targetIndex = SVRegistry.CurrentList.SelectedIndex + delta;
        targetIndex = Math.Clamp(targetIndex, 0, SVRegistry.CurrentList.MaxIndex);

        if(targetIndex == SVRegistry.CurrentList.SelectedIndex)
            return;

        selectedEntryCache[currentPath] = GetEntries(currentPath)[targetIndex];

        await Program.Renderer.EnqueueAction(async () =>
        {
            SVRegistry.CurrentList.SelectedIndex = targetIndex;

            await SVRegistry.CurrentList.Invalidate();
            await RefreshSelectionInfo();
            await RefreshPreview();
        });
    }

    public static Task HandleFile()
    {
        if(SelectedEntry is not null)
            switch (SelectedEntry.Value.Type)
            {
                case FSFileType.File:
                    Process.Start(
                        new ProcessStartInfo()
                        {
                            FileName = "xdg-open",
                            Arguments = SelectedEntry.Value.Path,
                            UseShellExecute = false,
                            RedirectStandardOutput = true,
                            RedirectStandardError = true,
                            CreateNoWindow = true,
                        }
                    );
                    break;
            }

        return Task.CompletedTask;
    }
}
