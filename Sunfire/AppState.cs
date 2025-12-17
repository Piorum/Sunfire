using System.Diagnostics;
using Sunfire.Registries;
using Sunfire.FSUtils.Models;
using Sunfire.Logging;

namespace Sunfire;

public static class AppState
{
    private static string currentPath = "";

    public static async Task ToggleHidden()
    {
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
        await(await GetSelectedEntry() is var selectedEntry && selectedEntry is not null && selectedEntry.Value.IsDirectory 
            ? Refresh(selectedEntry.Value.Path) 
            : HandleFile());

    //Nav Helpers
    public static async Task NavList(int delta)
    {
        await SVRegistry.CurrentList.Nav(delta);

        await RefreshPreviews();
    }

    public static async Task HandleFile()
    {
        if(await GetSelectedEntry() is var selectedEntry && selectedEntry is not null && !selectedEntry.Value.IsDirectory)
        {
            var (handler, args) = MediaRegistry.GetOpener(selectedEntry.Value);

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

    private static async Task<FSEntry?> GetSelectedEntry() => 
        await SVRegistry.CurrentList.GetCurrentEntry();

    private static async Task RefreshPreviews()
    {
        var selectedEntry = await GetSelectedEntry();

        await SVRegistry.PreviewView.Update(selectedEntry);
        await SVRegistry.SelectionInfoView.Update(selectedEntry);
    }
}
