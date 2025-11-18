using System.Diagnostics;
using Sunfire.Registries;
using Sunfire.Views;
using Sunfire.FSUtils;
using Sunfire.Views.Text;

namespace Sunfire;

public static class AppState
{
    private static readonly FSCache fsCache = new();
    private static readonly Dictionary<string, int> indexCache = [];

    private static string currentPath = "";
    private static FSEntry SelectedEntry => fsCache.GetEntries(currentPath)[SVRegistry.CurrentList.SelectedIndex];

    public static async Task Init()
    {
        //Swap for finding directory program is opened in?
        var userProfleDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        if (!Directory.Exists(userProfleDir)) throw new("User profile is not a directory");
        indexCache.Add(userProfleDir, 0);
        indexCache.Add("/home", 0);
        indexCache.Add("/", 4);

        await Refresh(userProfleDir);
    }

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

        switch(SelectedEntry.Type)
        {
            case FSFileType.Directory:
                ListSV previewList = new();

                await UpdateList(previewList, SelectedEntry.Path);

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
        SVRegistry.BottomLeftBorder.TitleLabel ??= new();
        SVRegistry.BottomLeftBorder.TitleLabel.Text = SelectedEntry.Path;

        //Misc File Info
        switch(SelectedEntry.Type)
        {
            case FSFileType.Directory:
                SVRegistry.BottomLeftLabel.Text = $" Directory {Directory.GetFiles(SelectedEntry.Path).Length + Directory.GetDirectories(SelectedEntry.Path).Length}";
                break;
            case FSFileType.File:
                SVRegistry.BottomLeftLabel.Text = $" Size: {new FileInfo(SelectedEntry.Path).Length}B";
                break;
        }

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

        var entries = fsCache.GetEntries(path);
        foreach (var entry in entries)
        {
            LabelSVSlim label = new() { Text = entry.Name };
            if(entry.Type == FSFileType.Directory)
                label.TextProperties |= Ansi.Models.SAnsiProperty.Bold;
            
            await list.AddLabel(label);
        }

        list.SelectedIndex = GetPreviousIndex(path, list.MaxIndex);

        await list.Invalidate();
    }

    private static int GetPreviousIndex(string path, int maxIndex)
    {
        indexCache.TryGetValue(path, out var previousIndex);
        if(previousIndex <= maxIndex && previousIndex > 0)
        {
            return previousIndex;
        }
        else
        {
            indexCache[path] = 0;
            return 0;
        }
    }

    public static async Task NavUp() => await NavList(-1);
    public static async Task NavDown() => await NavList(1);
    public static async Task NavOut() => 
        await(Directory.GetParent(currentPath) is var dirInfo && dirInfo is not null 
            ? Program.Renderer.EnqueueAction(() => Refresh(dirInfo.FullName)) 
            : Task.CompletedTask);
    public static async Task NavIn() => 
        await(SelectedEntry.Type == FSFileType.Directory 
            ? Program.Renderer.EnqueueAction(() => Refresh(SelectedEntry.Path)) 
            : HandleFile());

    //Nav Helpers
    public static async Task NavList(int delta)
    {
        var targetIndex = SVRegistry.CurrentList.SelectedIndex + delta;
        targetIndex = Math.Clamp(targetIndex, 0, SVRegistry.CurrentList.MaxIndex);

        if(targetIndex == SVRegistry.CurrentList.SelectedIndex)
            return;

        await Program.Renderer.EnqueueAction(async () =>
        {
            SVRegistry.CurrentList.SelectedIndex = targetIndex;
            indexCache[currentPath] = targetIndex;

            await SVRegistry.CurrentList.Invalidate();
            await RefreshSelectionInfo();
            await RefreshPreview();
        });
    }

    public static Task HandleFile()
    {
        switch (SelectedEntry.Type)
        {
            case FSFileType.File:
                Process.Start(
                    new ProcessStartInfo()
                    {
                        FileName = "xdg-open",
                        Arguments = SelectedEntry.Path,
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
