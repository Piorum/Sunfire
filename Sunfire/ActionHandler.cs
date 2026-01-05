using System.Diagnostics;
using Sunfire.FSUtils;
using Sunfire.Logging;
using Sunfire.Registries;
using Sunfire.Views;
using Sunfire.Views.Text;

namespace Sunfire;

public static class ActionHandler
{

    private static readonly Dictionary<string, Func<string, Task>> _actions = new() 
    { 
        { nameof(Copy).ToLower(), Copy },
        { nameof(Cut).ToLower(), Cut },
        { nameof(Delete).ToLower(), Delete },
    };

    public static async Task Run(string action, string cwd)
    {
        if(_actions.TryGetValue(action.ToLower(), out var task))
            await task(cwd);
    }

    private static async Task Copy(string cwd)
    {
        await Logger.Debug(nameof(Sunfire), $"Trying {nameof(Copy)} Action");

        if(cwd is null || !Directory.Exists(cwd))
            return;

        var entriesToCopy = AppState.TaggedEntries;
        if(entriesToCopy.Count == 0)
            return;

        var confirmed = await ConfirmationDialogue($"Copy {entriesToCopy.Count} Entries?");

        if(!confirmed)
            return;

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
            await AppState.InvalidateState();
        });
    }
    private static async Task Cut(string cwd)
    {
        await Logger.Debug(nameof(Sunfire), $"Trying {nameof(Cut)} Action");

        if(cwd is null || !Directory.Exists(cwd))
            return;

        var entriesToCut = AppState.TaggedEntries;
        if(entriesToCut.Count == 0)
            return;

        var confirmed = await ConfirmationDialogue($"Cut {entriesToCut.Count}?");

        if(!confirmed)
            return;

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
            await AppState.InvalidateState();
        });

    }
    private static async Task Delete(string cwd)
    {
        await Logger.Debug(nameof(Sunfire), $"Trying {nameof(Delete)} Action");

        var entriesToDelete = AppState.TaggedEntries;
        if(entriesToDelete.Count == 0)
            return;

        var confirmed = await ConfirmationDialogue($"Delete {entriesToDelete.Count} Entries?");

        if(!confirmed)
            return;

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
            await AppState.InvalidateState();
        });
    }

    private static async Task<bool> ConfirmationDialogue(string dialogue)
    {
        LabelSV label = new()
        {
            Y = 2
        };
        BorderSV border = new()
        {
            SubView = label
        };

        label.Segments = [ new() { Text = $" {dialogue} (Y/N)" } ];

        await Program.Renderer.EnqueueAction(async () => 
        {
            SVRegistry.RootPane.SubViews.Add(border);
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
            SVRegistry.RootPane.SubViews.Remove(border);
            await SVRegistry.RootPane.Invalidate();
        });

        return result;
    }
}
