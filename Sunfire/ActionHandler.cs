using System.Diagnostics;
using Sunfire.FSUtils;
using Sunfire.FSUtils.Models;
using Sunfire.Logging;
using Sunfire.Registries;
using Sunfire.Views;
using Sunfire.Views.Text;

namespace Sunfire;

public static class ActionHandler
{
    private static readonly TimeSpan errorTimeout = TimeSpan.FromSeconds(1);

    private static readonly Dictionary<string, Func<string, Task<ActionResult>>> _actions = new() 
    { 
        { nameof(Copy).ToLower(), Copy },
        { nameof(Cut).ToLower(), Cut },
        { nameof(Delete).ToLower(), Delete },
    };

    public static async Task Run(string action, string cwd)
    {
        ActionResult result;
        if(_actions.TryGetValue(action.ToLower(), out var task))
            result = await task(cwd);
        else
            result = new() { Success = false, errorMessage = "Invalid action." };

        if(result.Success)
            return;

        var errorView = InfoView.New("");

        errorView.UpdateInfo([new() { Text = $" {result.errorMessage}", Style = new() { ForegroundColor = ColorRegistry.Red }}]);

        await Program.Renderer.EnqueueAction(async () =>
        {
            SVRegistry.InfosView.SubViews.Add(errorView);
            await SVRegistry.RootPane.Invalidate();

            _ = Task.Run(async () =>
            {
               await Task.Delay(errorTimeout);

                await Program.Renderer.EnqueueAction(async () =>
                {
                   SVRegistry.InfosView.SubViews.Remove(errorView);
                   await SVRegistry.RootPane.Invalidate(); 
                });
            });
        });
    }

    private static async Task<ActionResult> Copy(string cwd)
    {
        await Logger.Debug(nameof(Sunfire), $"Trying {nameof(Copy)} Action");

        if(cwd is null || !Directory.Exists(cwd))
            return new() { Success = false, errorMessage = "Current working directory is null or does not exist."};

        List<FSEntry> entriesToCopy = [.. AppState.TaggedEntries];
        if(entriesToCopy.Count == 0)
            return new() { Success = false, errorMessage = "No tagged entries to perform action on."};

        var confirmed = await ConfirmationDialogue($"Copy {entriesToCopy.Count} Entries?");

        if(!confirmed)
            return new() { Success = false, errorMessage = "Action was not confirmed."};

        _ = Task.Run(async () =>
        {
            //Confirmed, cwd exists, and there are tagged entries
            var psi = new ProcessStartInfo()
            {
                FileName = "cp",
                ArgumentList = {
                    "-a",
                    "-t",
                    ".",
                    "--",
                },
                WorkingDirectory = cwd,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
            };

            foreach(var entry in entriesToCopy)
                psi.ArgumentList.Add(entry.Path);

            var copy = Process.Start(psi);

            copy?.WaitForExit();

            await AppState.ClearTags();

            SVRegistry.CurrentList.SaveCurrentEntry();

            var directoriesToInvalidate = entriesToCopy.Select(e => e.Directory).Distinct().ToList();
            directoriesToInvalidate.Add(cwd);
            foreach(var directory in directoriesToInvalidate)
            {
                FSCache.Clear();
                EntriesListView.ClearCache();
                //FSCache.Invalidate(directory);
                //EntriesListView.ClearCache(directory);
            }

            await AppState.Refresh();
        });

        return new() { Success = true };
    }
    private static async Task<ActionResult> Cut(string cwd)
    {
        await Logger.Debug(nameof(Sunfire), $"Trying {nameof(Cut)} Action");

        if(cwd is null || !Directory.Exists(cwd))
            return new() { Success = false, errorMessage = "Current working directory is null or does not exist."};

        List<FSEntry> entriesToCut = [.. AppState.TaggedEntries];
        if(entriesToCut.Count == 0)
            return new() { Success = false, errorMessage = "No tagged entries to perform action on."};

        var confirmed = await ConfirmationDialogue($"Cut {entriesToCut.Count}?");

        if(!confirmed)
            return new() { Success = false, errorMessage = "Action was not confirmed."};

        _ = Task.Run(async () =>
        {
            //Confirmed, cwd exists, and there are tagged entries
            var psi = new ProcessStartInfo()
            {
                FileName = "mv",
                ArgumentList = {
                    "-t",
                    ".",
                    "--",
                },
                WorkingDirectory = cwd,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
            };

            foreach(var entry in entriesToCut)
                psi.ArgumentList.Add(entry.Path);

            var cut = Process.Start(psi);

            cut?.WaitForExit();

            await AppState.ClearTags();

            SVRegistry.CurrentList.SaveCurrentEntry();

            var directoriesToInvalidate = entriesToCut.Select(e => e.Directory).Distinct().ToList();
            directoriesToInvalidate.Add(cwd);
            foreach(var directory in directoriesToInvalidate)
            {
                FSCache.Clear();
                EntriesListView.ClearCache();
                //FSCache.Invalidate(directory);
                //EntriesListView.ClearCache(directory);
            }

            await AppState.Refresh();
        });

        return new() { Success = true };
    }
    private static async Task<ActionResult> Delete(string cwd)
    {
        await Logger.Debug(nameof(Sunfire), $"Trying {nameof(Delete)} Action");

        string dialogue;
        List<FSEntry> entriesToDelete = [.. AppState.TaggedEntries];
        if(entriesToDelete.Count == 0)
        {
            var entryToDelete = SVRegistry.CurrentList.GetCurrentEntry();

            if(entryToDelete is null)
                return new() { Success = false, errorMessage = "No entries to perform action on."};

            entriesToDelete.Add(entryToDelete.Value);
            dialogue = $"Delete \"{entryToDelete.Value.Name}\"?";
        }
        else
        {
            dialogue = $"Delete {entriesToDelete.Count} Entries?";
        }

        var confirmed = await ConfirmationDialogue(dialogue);

        if(!confirmed)
            return new() { Success = false, errorMessage = "Action was not confirmed."};

        _ = Task.Run(async () =>
        {
            //Confirmed and there are tagged entries
            var psi = new ProcessStartInfo()
            {
                FileName = "rm",
                ArgumentList = {
                    "-rf",
                    "--",
                },
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
            };

            foreach(var entry in entriesToDelete)
                psi.ArgumentList.Add(entry.Path);

            var delete = Process.Start(psi);

            delete?.WaitForExit();

            await AppState.ClearTags();

            SVRegistry.CurrentList.SaveCurrentEntry();

            var directoriesToInvalidate = entriesToDelete.Select(e => e.Directory).Distinct().ToList();
            foreach(var directory in directoriesToInvalidate)
            {
                FSCache.Clear();
                EntriesListView.ClearCache();
                //FSCache.Invalidate(directory);
                //EntriesListView.ClearCache(directory);
            }

            await AppState.Refresh();
        });

        return new() { Success = true };
    }

    private static async Task<bool> ConfirmationDialogue(string dialogue)
    {
        var view = InfoView.New($" {dialogue} (Y/N)", null);

        await Program.Renderer.EnqueueAction(async () => 
        {
            SVRegistry.InfosView.SubViews.Add(view);
            await SVRegistry.RootPane.Invalidate();
        });        
        
        TaskCompletionSource<bool> tcs = new();
        bool result = false;
        await Program.InputHandler.EnableInputMode(
            textHandler: _ => { return Task.CompletedTask; },  
            deletionHandler: () => { return Task.CompletedTask; },            
            exitHandlers: 
            [
                (ConsoleKey.Escape, () => { return Task.CompletedTask; }),
                (ConsoleKey.N, () => { return Task.CompletedTask; }),
                (ConsoleKey.Y, () => { result = true; return Task.CompletedTask; }),
            ],
            specialHandlers: null
        );

        await Logger.Debug(nameof(Sunfire), $"Confirmation Result {result}");

        await Program.Renderer.EnqueueAction(async () => 
        {
            SVRegistry.InfosView.SubViews.Remove(view);
            await SVRegistry.RootPane.Invalidate();
        });

        return result;
    }

    private struct ActionResult
    {
        required public bool Success;
        public string errorMessage;
    }
}
