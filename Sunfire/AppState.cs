using System.Diagnostics;
using Sunfire.Registries;
using Sunfire.FSUtils.Models;
using Sunfire.Logging;
using Sunfire.FSUtils;
using Sunfire.Views;
using System.Text;
using Sunfire.Views.Text;
using Sunfire.Ansi.Models;

namespace Sunfire;

public static class AppState
{
    private static string currentPath = "";

    public static async Task ToggleHidden()
    {
        await SVRegistry.CurrentList.SaveCurrentEntry();

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

    public static async Task Refresh() => 
        await Refresh(currentPath);

    private static async Task Refresh(string path)
    {
        currentPath = path;
        var containerPath = Directory.GetParent(currentPath) is var dirInfo && dirInfo is not null 
            ? dirInfo.FullName 
            : "";

        await SVRegistry.CurrentList.SaveCurrentEntry();

        await SVRegistry.ContainerList.UpdateCurrentPath(containerPath);
        await SVRegistry.CurrentList.UpdateCurrentPath(currentPath);

        await RefreshPreviews();
    }

    public static async Task NavUp() => await NavList(-1);
    public static async Task NavDown() => await NavList(1);
    public static async Task NavOut() => 
        await(Directory.GetParent(currentPath) is var dirInfo && dirInfo is not null 
            ? Refresh(dirInfo.FullName) 
            : Task.CompletedTask);
    public static async Task NavIn() => 
        await(await GetCurrentEntry() is var currentEntry && currentEntry is not null && currentEntry.Value.IsDirectory 
            ? Refresh(currentEntry.Value.Path) 
            : HandleFile());

    //Nav Helpers
    public static async Task NavList(int delta)
    {
        await SVRegistry.CurrentList.Nav(delta);

        await RefreshPreviews();
    }

    public static async Task HandleFile()
    {
        if(await GetCurrentEntry() is var currentEntry && currentEntry is not null && !currentEntry.Value.IsDirectory)
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
    }

    public static async Task Search()
    {
        FSEntry? startEntry = await SVRegistry.CurrentList.GetCurrentEntry();
        List<FSEntry> currentEntries = await FSCache.GetEntries(currentPath, CancellationToken.None);

        char preCharacter = '/';

        FSEntry? bestMatch = null;
        bool invalidSearch = false;

        async Task Search(string input)
        {
            bestMatch = GetBestMatch(currentEntries, input);
            invalidSearch = bestMatch is null;

            if(!invalidSearch)
            {
                await SVRegistry.CurrentList.Nav(bestMatch);
                await RefreshPreviews();
            }
        }

        await EnableInputMode(
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

    //Input Mode Helpers
    public static async Task<string> EnableInputMode(char preCharacter, Func<bool> warnSource, Func<string, Task> onUpdate, List<(ConsoleKey key, Func<Task> task)> exitHandlers, List<(ConsoleKey key, Func<Task> task)> specialHandlers)
    {
        StringBuilder text = new();

        var textDisplay = await AddTextDisplay();
        await UpdateTextDisplay(textDisplay, preCharacter, text.ToString(), warnSource());

        List<(ConsoleKey key, Func<Task> task)> completeExitHandlers = [];
        foreach(var (key, task) in exitHandlers)
        {
            completeExitHandlers.Add((key, async () =>
            {
                await task();
                await RemoveTextDisplay(textDisplay);
            }));
        }

        Program.InputHandler.EnableInputMode(
            textHandler: async (a) =>
            {
                text.Append(a);
                var textString = text.ToString();

                await onUpdate(textString);
                await UpdateTextDisplay(textDisplay, preCharacter, textString, warnSource());
            },
            deletionHandler: async () => 
            {
                if(text.Length > 0)
                    text.Remove(text.Length - 1, 1);
                var textString = text.ToString();

                await onUpdate(textString);
                await UpdateTextDisplay(textDisplay, preCharacter, textString, warnSource());
            },            
            exitHandlers: completeExitHandlers,
            specialHandlers
        );

        return text.ToString();
    }

    private static async Task<(BorderSV searchTextLabelBorder, LabelSV searchTextLabel)> AddTextDisplay()
    {
        LabelSV searchTextLabel = new()
        {
            Y = 2
        };
        BorderSV searchTextLabelBorder = new()
        {
            SubView = searchTextLabel
        };

        await Program.Renderer.EnqueueAction(async () => 
        {
            SVRegistry.RootPane.SubViews.Add(searchTextLabelBorder);
            await SVRegistry.RootPane.Invalidate();
        });

        return (searchTextLabelBorder, searchTextLabel);
    }

    private static async Task RemoveTextDisplay((BorderSV searchTextLabelBorder, LabelSV searchTextLabel) textDisplay)
    {
        await Program.Renderer.EnqueueAction(async () => 
        {
            SVRegistry.RootPane.SubViews.Remove(textDisplay.searchTextLabelBorder);
            await SVRegistry.RootPane.Invalidate();
        });
    }

    private static async Task UpdateTextDisplay((BorderSV searchTextLabelBorder, LabelSV searchTextLabel) textDisplay, char preCharacter, string text, bool warn)
    {
        SStyle baseStyle = new() { ForegroundColor = warn ? ColorRegistry.Red : null };

        var segments = new LabelSVSlim.LabelSegment[2]
        {
            new() { Text = $" {preCharacter}{text}", Style = baseStyle },
            new() { Text = " ", Style = baseStyle with { Properties = SAnsiProperty.Underline } }
        };

        await Program.Renderer.EnqueueAction(async () =>
        {
            textDisplay.searchTextLabel.Segments = segments;
            await textDisplay.searchTextLabel.Invalidate();
        });
    }

    //Search Helpers
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

    private static async Task<FSEntry?> GetCurrentEntry() => 
        await SVRegistry.CurrentList.GetCurrentEntry();

    private static async Task RefreshPreviews()
    {
        var currentEntry = await GetCurrentEntry();

        await SVRegistry.PreviewView.Update(currentEntry);
        await SVRegistry.SelectionInfoView.Update(currentEntry);
    }
}
