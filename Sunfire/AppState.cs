using System.Diagnostics;
using Sunfire.Registries;
using Sunfire.FSUtils.Models;
using Sunfire.Logging;
using Sunfire.FSUtils;
using Sunfire.Views;

namespace Sunfire;

public static class AppState
{
    private static string currentPath = "";
    private static readonly InputModeHook inputModeHook = new(SVRegistry.RootPane);

    private static readonly List<FSEntry> taggedEntries = [];

    public static async Task ToggleHidden()
    {
        SVRegistry.CurrentList.SaveCurrentEntry();

        await SVRegistry.ContainerList.ToggleHidden();
        await SVRegistry.CurrentList.ToggleHidden();
        await SVRegistry.PreviewView.directoryPreviewer.ToggleHidden();

        await Refresh();
        await Logger.Info(nameof(Sunfire), "Toggled Hidden Entries");
    }

    public static async Task Init()
    {
        string basePath;
        if(Program.Options.UseUserProfileAsDefault)
            basePath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        else
            basePath = Environment.CurrentDirectory;

        if (!Directory.Exists(basePath)) 
            throw new("basePath is invalid, Not a directory");

        var curDir = new DirectoryInfo(basePath);

        while(curDir?.Parent != null)
        {
            string containerPath = curDir.Parent.FullName;
            string entryToSelectName = curDir.Name;

            var entries = await FSCache.GetEntries(containerPath, default);

            var entry = entries.FirstOrDefault(e => e.Name == entryToSelectName);

            EntriesListView.SaveEntry(containerPath, entry);

            curDir = curDir.Parent;
        }

        //Populating Views && currentPath
        await Refresh(basePath);
    }

    public static async Task InvalidateState()
    {
        SVRegistry.CurrentList.SaveCurrentEntry();

        FSCache.Clear();
        EntriesListView.ClearCache();

        await Refresh();
    }
    private static async Task Refresh() => 
        await Refresh(currentPath);

    private static async Task Refresh(string path)
    {
        currentPath = path;
        var containerPath = Directory.GetParent(currentPath) is var dirInfo && dirInfo is not null 
            ? dirInfo.FullName 
            : "";

        SVRegistry.CurrentList.SaveCurrentEntry();

        await SVRegistry.ContainerList.UpdateCurrentPath(containerPath);
        await SVRegistry.CurrentList.UpdateCurrentPath(currentPath);

        await RefreshPreviews();
    }

    //Refresh Helpers
    private static FSEntry? GetCurrentEntry() => 
        SVRegistry.CurrentList.GetCurrentEntry();

    private static async Task RefreshPreviews()
    {
        var currentEntry = GetCurrentEntry();

        await SVRegistry.PreviewView.Update(currentEntry);
        await SVRegistry.SelectionInfoView.Update(currentEntry);
    }

    //Nav
    public static async Task NavUp() => await NavList(-1);
    public static async Task NavDown() => await NavList(1);
    public static async Task NavOut() => 
        await(Directory.GetParent(currentPath) is var dirInfo && dirInfo is not null 
            ? Refresh(dirInfo.FullName) 
            : Task.CompletedTask);
    public static async Task NavIn() => 
        await(GetCurrentEntry() is var currentEntry && currentEntry is not null && currentEntry.Value.IsDirectory 
            ? Refresh(currentEntry.Value.Path) 
            : HandleFile());

    //Nav Helpers
    public static async Task NavList(int delta)
    {
        await SVRegistry.CurrentList.Nav(delta);

        await RefreshPreviews();
    }

    //Actions
    public static async Task Tag()
    {
        var (entry, enabled) = await SVRegistry.CurrentList.ToggleOrUpdateCurrentEntryTag(ColorRegistry.Yellow);

        if(entry is not null)
        {
            if(enabled)
                taggedEntries.Add(entry.Value);
            else
                taggedEntries.Remove(entry.Value);

            await NavDown();
        }
    }
    public static async Task ClearTags()
    {
        taggedEntries.Clear();
        EntriesListView.ClearTags();
        
        await Program.Renderer.EnqueueAction(async () =>
        {
            await SVRegistry.ContainerList.Invalidate();
            await SVRegistry.CurrentList.Invalidate();
            await SVRegistry.PreviewView.Invalidate(); 
        });
    }
    public static Task HandleFile()
    {
        if(GetCurrentEntry() is var currentEntry && currentEntry is not null && !currentEntry.Value.IsDirectory)
        {
            var (handler, args) = MediaRegistry.GetOpener(currentEntry.Value);

            //Launched detached
            Process.Start(
                new ProcessStartInfo()
                {
                    FileName = "sh",
                    Arguments = $"-c \"setsid {handler} {args} >/dev/null 2>&1 </dev/null &\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                }
            );
        }

        return Task.CompletedTask;
    }
    public static async Task Search()
    {
        FSEntry? startEntry = SVRegistry.CurrentList.GetCurrentEntry();
        List<FSEntry> currentEntries = await FSCache.GetEntries(currentPath, CancellationToken.None);
        EntrySearcher searcher = new(currentEntries);

        char preCharacter = '/';

        FSEntry? bestMatch = null;
        bool invalidSearch = false;

        async Task Search(string input)
        {
            bestMatch = searcher.GetBestMatch(input);
            invalidSearch = bestMatch is null;

            if(!invalidSearch)
            {
                await SVRegistry.CurrentList.Nav(bestMatch);
                await RefreshPreviews();
            }
        }

        await inputModeHook.EnableInputMode(
            title: "Search",
            preCharacter: preCharacter, 
            warnSource: () => invalidSearch, 
            onUpdate: Search, 
            exitHandlers: [ 
                (ConsoleKey.Escape, () => SVRegistry.CurrentList.Nav(startEntry)), 
                (ConsoleKey.Tab, () => Task.CompletedTask), 
                (ConsoleKey.Enter, () => Task.CompletedTask)
            ], 
            specialHandlers: []
        );
    }
    public static async Task Sh()
    {
        char preCharacter = '$';
        bool cancelled = false;

        var cmd = await inputModeHook.EnableInputMode(
            title: "Shell",
            preCharacter: preCharacter,
            warnSource: () => false,
            onUpdate: (_) => Task.CompletedTask,
            exitHandlers:[
                (ConsoleKey.Escape, () => { cancelled = true; return Task.CompletedTask; }), 
                (ConsoleKey.Tab, () => Task.CompletedTask), 
                (ConsoleKey.Enter, () => Task.CompletedTask)
            ], 
            specialHandlers: []
        );

        if(cancelled)
            return;

        await Logger.Debug(nameof(Sunfire), $"Running shell command: \"{cmd}\" in \"{currentPath}\"");
        var bash = Process.Start(
            new ProcessStartInfo()
            {
                FileName = "sh",
                Arguments = $"-c \"{cmd}\"",
                WorkingDirectory = currentPath,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
            }
        );

        _ = Task.Run(async () =>
        {
            bash?.WaitForExit();
            await InvalidateState();
        });
    }
}
