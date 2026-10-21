using System.Diagnostics;
using Sunfire.Registries;
using Sunfire.FSUtils.Models;
using Moonfire.Logging;
using Sunfire.FSUtils;
using Sunfire.Views;

namespace Sunfire;

public static class AppState
{
    public static readonly List<FSEntry> TaggedEntries = [];

    private static string currentPath = "";
    private static readonly InputModeHook inputModeHook = new();

    public static void ToggleHidden()
    {
        SVRegistry.CurrentList.SaveCurrentEntry();

        SVRegistry.ContainerList.ToggleHidden();
        SVRegistry.CurrentList.ToggleHidden();
        SVRegistry.PreviewView.directoryPreviewer.ToggleHidden();

        Refresh();
        Logger.Info(nameof(Sunfire), "Toggled Hidden Entries");
    }

    public static void Init()
    {
        string basePath;
        if(Program.Options.UseUserProfileAsDefault)
            basePath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        else
            basePath = Environment.CurrentDirectory;

        if (!Directory.Exists(basePath)) 
            throw new("basePath is invalid, Not a directory");

        //Populating Views && currentPath
        Refresh(basePath);
    }

    public static void InvalidateState()
    {
        SVRegistry.CurrentList.SaveCurrentEntry();

        FSCache.Clear();
        EntriesListView.ClearCache();

        Refresh();
    }
    public static void Refresh() => 
        Refresh(currentPath);

    private static void Refresh(string path)
    {
        currentPath = path;
        var containerPath = Directory.GetParent(currentPath) is var dirInfo && dirInfo is not null 
            ? dirInfo.FullName 
            : "";

        SVRegistry.CurrentList.SaveCurrentEntry();

        Parallel.Invoke(
            () => SVRegistry.ContainerList.UpdateCurrentPath(containerPath, Path.GetFileName(currentPath)),
            () => SVRegistry.CurrentList.UpdateCurrentPath(currentPath)
        );

        RefreshPreviews();
    }

    //Refresh Helpers
    private static FSEntry? GetCurrentEntry() => 
        SVRegistry.CurrentList.GetCurrentEntry();

    private static void RefreshPreviews()
    {
        var currentEntry = GetCurrentEntry();

        SVRegistry.PreviewView.Update(currentEntry);
        SVRegistry.SelectionInfoView.Update(currentEntry);
    }

    //Nav
    public static void NavUp() => NavList(-1);
    public static void NavDown() => NavList(1);
    public static void NavOut()
    {
        if (Directory.GetParent(currentPath) is { } dir)
        {
            Refresh(dir.FullName);
        }
    }
    public static void NavIn()
    {
        if (GetCurrentEntry() is not { } currentEntry)
            return;

        if (currentEntry.IsDirectory)
            Refresh(currentEntry.Path);
        else
            HandleFile();
    }

    //Nav Helpers
    public static void NavList(int delta)
    {
        SVRegistry.CurrentList.Nav(delta);

        RefreshPreviews();
    }

    //Actions
    public static void Tag()
    {
        var (entry, enabled) = SVRegistry.CurrentList.ToggleOrUpdateCurrentEntryTag(ColorRegistry.Yellow);

        if(entry is not null)
        {
            if(enabled)
                TaggedEntries.Add(entry.Value);
            else
                TaggedEntries.Remove(entry.Value);

            NavDown();
        }
    }
    public static void ClearTags()
    {
        TaggedEntries.Clear();
        EntriesListView.ClearTags();
        
        Program.App.Renderer.EnqueueAction(() =>
        {
            SVRegistry.ContainerList.Invalidate();
            SVRegistry.CurrentList.Invalidate();
            SVRegistry.PreviewView.Invalidate(); 
        });
    }
    public static void HandleFile()
    {
        if(GetCurrentEntry() is var currentEntry && currentEntry is not null && !currentEntry.Value.IsDirectory)
        {
            var opener = MediaRegistry.GetOpener(currentEntry.Value);

            static string ShellQuote(string s) =>
                "'" + s.Replace("'", "'\"'\"'") + "'";
            
            //Launched detached
            Process.Start(
                new ProcessStartInfo()
                {
                    FileName = "sh",
                    ArgumentList =
                    {
                        "-c",
                        $"setsid {ShellQuote(opener.handler)} {ShellQuote(opener.args(currentEntry.Value))} >/dev/null 2>&1 </dev/null &" 
                    },
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                }
            );
        }
    }
    public static void Search()
    {
        FSEntry? startEntry = SVRegistry.CurrentList.GetCurrentEntry();
        List<FSEntry> currentEntries = FSCache.GetEntries(currentPath);
        EntrySearcher searcher = new(currentEntries);

        bool invalidSearch = false;

        inputModeHook.Prompt(
            title: "Search",
            preCharacter: '/',
            warnSource: () => invalidSearch,
            onUpdate: input =>
            {
                var bestMatch = searcher.GetBestMatch(input);
                invalidSearch = bestMatch is null;

                if (!invalidSearch)
                {
                    SVRegistry.CurrentList.Nav(bestMatch);
                    RefreshPreviews();
                }
            },
            onCancel: () =>
            {
                // Restore entry on Esc
                SVRegistry.CurrentList.Nav(startEntry);
                RefreshPreviews();
            }
        );
    }
    public static void Action()
    {
        inputModeHook.Prompt(
            title: "Action",
            preCharacter: ':',
            onSubmit: action =>
            {
                Logger.Debug(nameof(Sunfire), $"Trying to parse action: \"{action}\"");
                ActionHandler.Run(action, currentPath);
            }
        );
    }
    public static void Sh()
    {
        inputModeHook.Prompt(
            title: "Shell",
            preCharacter: '$',
            onSubmit: cmd =>
            {
                Logger.Debug(nameof(Sunfire), $"Running shell command: \"{cmd}\" in \"{currentPath}\"");

                var bash = Process.Start(new ProcessStartInfo
                {
                    FileName = "sh",
                    ArgumentList = { "-c", cmd },
                    WorkingDirectory = currentPath,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                });

                ThreadPool.QueueUserWorkItem(_ =>
                {
                    bash?.WaitForExit();
                    InvalidateState();
                });
            }
        );
    }
}
