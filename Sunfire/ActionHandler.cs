using System.Diagnostics;
using Moonfire.Logging;
using Sunfire.FSUtils;
using Sunfire.FSUtils.Models;
using Sunfire.Registries;
using Sunfire.Views;

namespace Sunfire;

public static class ActionHandler
{
    private static readonly TimeSpan errorTimeout = TimeSpan.FromSeconds(1);

    private static readonly Dictionary<string, Func<string, ActionResult>> _actions = new(StringComparer.OrdinalIgnoreCase)
    { 
        { nameof(Copy), Copy },
        { nameof(Cut), Cut },
        { nameof(Delete), Delete },
    };

    public static void Run(string action, string cwd)
    {
        ActionResult result;
        if (_actions.TryGetValue(action, out var actionFunc))
            result = actionFunc(cwd);
        else
            result = new() { Success = false, errorMessage = "Invalid action." };

        if (result.Success)
            return;

        ShowErrorMessage(result.errorMessage);
    }

    private static void ShowErrorMessage(string message)
    {
        var errorView = InfoView.New("");
        errorView.UpdateInfo([new() { Text = $" {message}", Style = new() { ForegroundColor = ColorRegistry.Red } }]);

        Program.App.Renderer.EnqueueAction(() =>
        {
            SVRegistry.InfosView.SubViews.Add(errorView);
            SVRegistry.RootPane.Invalidate();

            // Replace Task.Delay with ThreadPool worker
            ThreadPool.QueueUserWorkItem(_ =>
            {
                Thread.Sleep(errorTimeout);

                Program.App.Renderer.EnqueueAction(() =>
                {
                    SVRegistry.InfosView.SubViews.Remove(errorView);
                    SVRegistry.RootPane.Invalidate();
                });
            });
        });
    }

    private static ActionResult Copy(string cwd)
    {
        Logger.Debug(nameof(Sunfire), $"Trying {nameof(Copy)} Action");

        if (cwd is null || !Directory.Exists(cwd))
            return new() { Success = false, errorMessage = "Current working directory is null or does not exist." };

        List<FSEntry> entriesToCopy = [.. AppState.TaggedEntries];
        if (entriesToCopy.Count == 0)
            return new() { Success = false, errorMessage = "No tagged entries to perform action on." };

        var confirmed = ConfirmationDialogue($"Copy {entriesToCopy.Count} Entries?");
        if (!confirmed)
            return new() { Success = false, errorMessage = "Action was not confirmed." };

        ThreadPool.QueueUserWorkItem(_ =>
        {
            var psi = new ProcessStartInfo
            {
                FileName = "cp",
                ArgumentList = { "-a", "-t", ".", "--" },
                WorkingDirectory = cwd,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
            };

            foreach (var entry in entriesToCopy)
                psi.ArgumentList.Add(entry.Path);

            var copy = Process.Start(psi);
            copy?.WaitForExit();

            AppState.ClearTags();
            SVRegistry.CurrentList.SaveCurrentEntry();

            var directoriesToInvalidate = entriesToCopy.Select(e => e.Directory).Distinct().ToList();
            directoriesToInvalidate.Add(cwd);
            foreach (var directory in directoriesToInvalidate)
            {
                FSCache.Invalidate(directory);
                EntriesListView.ClearCache(directory);
            }

            AppState.Refresh();
        });

        return new() { Success = true };
    }

    private static ActionResult Cut(string cwd)
    {
        Logger.Debug(nameof(Sunfire), $"Trying {nameof(Cut)} Action");

        if (cwd is null || !Directory.Exists(cwd))
            return new() { Success = false, errorMessage = "Current working directory is null or does not exist." };

        List<FSEntry> entriesToCut = [.. AppState.TaggedEntries];
        if (entriesToCut.Count == 0)
            return new() { Success = false, errorMessage = "No tagged entries to perform action on." };

        var confirmed = ConfirmationDialogue($"Cut {entriesToCut.Count}?");
        if (!confirmed)
            return new() { Success = false, errorMessage = "Action was not confirmed." };

        ThreadPool.QueueUserWorkItem(_ =>
        {
            var psi = new ProcessStartInfo
            {
                FileName = "mv",
                ArgumentList = { "-t", ".", "--" },
                WorkingDirectory = cwd,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
            };

            foreach (var entry in entriesToCut)
                psi.ArgumentList.Add(entry.Path);

            var cut = Process.Start(psi);
            cut?.WaitForExit();

            AppState.ClearTags();
            SVRegistry.CurrentList.SaveCurrentEntry();

            var directoriesToInvalidate = entriesToCut.Select(e => e.Directory).Distinct().ToList();
            directoriesToInvalidate.Add(cwd);
            foreach (var directory in directoriesToInvalidate)
            {
                FSCache.Invalidate(directory);
                EntriesListView.ClearCache(directory);
            }

            AppState.Refresh();
        });

        return new() { Success = true };
    }

    private static ActionResult Delete(string cwd)
    {
        Logger.Debug(nameof(Sunfire), $"Trying {nameof(Delete)} Action");

        string dialogue;
        List<FSEntry> entriesToDelete = [.. AppState.TaggedEntries];
        if (entriesToDelete.Count == 0)
        {
            var entryToDelete = SVRegistry.CurrentList.GetCurrentEntry();
            if (entryToDelete is null)
                return new() { Success = false, errorMessage = "No entries to perform action on." };

            entriesToDelete.Add(entryToDelete.Value);
            dialogue = $"Delete \"{entryToDelete.Value.Name}\"?";
        }
        else
        {
            dialogue = $"Delete {entriesToDelete.Count} Entries?";
        }

        var confirmed = ConfirmationDialogue(dialogue);
        if (!confirmed)
            return new() { Success = false, errorMessage = "Action was not confirmed." };

        ThreadPool.QueueUserWorkItem(_ =>
        {
            var psi = new ProcessStartInfo
            {
                FileName = "rm",
                ArgumentList = { "-rf", "--" },
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
            };

            foreach (var entry in entriesToDelete)
                psi.ArgumentList.Add(entry.Path);

            var delete = Process.Start(psi);
            delete?.WaitForExit();

            AppState.ClearTags();
            SVRegistry.CurrentList.SaveCurrentEntry();

            var directoriesToInvalidate = entriesToDelete.Select(e => e.Directory).Distinct().ToList();
            foreach (var directory in directoriesToInvalidate)
            {
                FSCache.Invalidate(directory);
                EntriesListView.ClearCache(directory);
            }

            AppState.Refresh();
        });

        return new() { Success = true };
    }

    private static bool ConfirmationDialogue(string dialogue)
    {
        var view = InfoView.New($" {dialogue} (Y/N)", null);

        Program.App.Renderer.EnqueueAction(() => 
        {
            SVRegistry.InfosView.SubViews.Add(view);
            SVRegistry.RootPane.Invalidate();
        });

        using var rawInput = Program.App.InputHandler.OpenRaw();
        bool result = false;

        try
        {
            while (true)
            {
                var input = rawInput.Take();
                var key = input.Key.KeyboardKey;

                if (key == ConsoleKey.Y)
                {
                    result = true;
                    break;
                }

                if (key is ConsoleKey.N or ConsoleKey.Escape)
                {
                    result = false;
                    break;
                }
            }
        }
        finally
        {
            Program.App.Renderer.EnqueueAction(() => 
            {
                SVRegistry.InfosView.SubViews.Remove(view);
                SVRegistry.RootPane.Invalidate();
            });
        }

        Logger.Debug(nameof(Sunfire), $"Confirmation Result: {result}");
        return result;
    }

    private struct ActionResult
    {
        public required bool Success;
        public string errorMessage;
    }
}