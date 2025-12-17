using System.Diagnostics;
using Sunfire.Registries;
using Sunfire.Views;
using Sunfire.FSUtils;
using Sunfire.FSUtils.Models;
using Sunfire.Logging;
using Sunfire.Tui.Interfaces;

namespace Sunfire;

public static class AppState
{
    public static readonly FSCache fsCache = new();

    private static string currentPath = "";

    private static CancellationTokenSource? previewGenCts;

    private static CancellationToken SecurePreviewGenToken()
    {
        previewGenCts?.Cancel();
        previewGenCts?.Dispose();
        
        previewGenCts = new();
        return previewGenCts.Token;
    }

    public static async Task ToggleHidden()
    {
        await SVRegistry.ContainerList.ToggleHidden();
        await SVRegistry.CurrentList.ToggleHidden();
        await previewEntriesList.ToggleHidden();

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

    private static async Task<FSEntry?> GetSelectedEntry() => 
        await SVRegistry.CurrentList.GetCurrentEntry();

    public static async Task Refresh() => 
        await Refresh(currentPath);

    private static async Task Refresh(string path)
    {
        currentPath = path;

        await RefreshContainerList();
        await RefreshCurrentList();

        var previewToken = SecurePreviewGenToken();

        var selectedEntry = await GetSelectedEntry();

        if(selectedEntry is null)
            await Logger.Debug(nameof(Sunfire), $"selectedEntry is null at {currentPath}");

        try
        {   
            var preview = await GetPreview(selectedEntry, previewToken);

            if(!previewToken.IsCancellationRequested)
                await Program.Renderer.EnqueueAction(async () =>
                {

                    await RefreshPreview(preview);
                    await RefreshDirectoryHint();
                    await RefreshSelectionInfo(selectedEntry);
                });
            }
        catch (OperationCanceledException) { }
    }

    private static async Task RefreshContainerList() => 
        await (Directory.GetParent(currentPath) is var dirInfo && dirInfo is null 
            ? SVRegistry.ContainerList.UpdateCurrentPath("") 
            : SVRegistry.ContainerList.UpdateCurrentPath(dirInfo.FullName));

    private static async Task RefreshCurrentList() =>
        await SVRegistry.CurrentList.UpdateCurrentPath(currentPath);

    private static Process? previewer = null;
    private static bool clean = true;
    private static async Task RefreshPreview(IRelativeSunfireView? view)
    {
        SVRegistry.PreviewPane.SubViews.Clear();

        if(view is not null)
        {
            if(!clean)
            {
                await Logger.Debug(nameof(Sunfire), "Cleaning");

                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                string cleanerPath = Path.Combine(baseDir, "sunfire-kitty-cleaner");
                
                var cleaner = new Process
                {
                    StartInfo = new ProcessStartInfo()
                    {
                        FileName = cleanerPath,
                        UseShellExecute = false,
                        RedirectStandardOutput = false,
                        RedirectStandardError = true,
                        CreateNoWindow = true,
                    }
                };
                
                cleaner.Start();

                if(previewer is not null && !previewer.HasExited)
                {
                    var oldPreviewer = previewer;
                    previewer = null;
                    _ = Task.Run(async () =>
                    {
                        oldPreviewer.Kill(true);
                        oldPreviewer.Dispose();

                        await Program.Renderer.EnqueueAction(async () => 
                        {
                            Program.Renderer.Clear(SVRegistry.PreviewPane.OriginX, SVRegistry.PreviewPane.OriginY, SVRegistry.PreviewPane.SizeX, SVRegistry.PreviewPane.SizeY);
                            await SVRegistry.PreviewPane.Invalidate();
                        });
                    });
                }
                else
                {
                    Program.Renderer.Clear(SVRegistry.PreviewPane.OriginX, SVRegistry.PreviewPane.OriginY, SVRegistry.PreviewPane.SizeX, SVRegistry.PreviewPane.SizeY);
                    await SVRegistry.PreviewPane.Invalidate();
                }

                clean = true;
            }

            SVRegistry.PreviewPane.SubViews.Add(view);
            
            await SVRegistry.PreviewPane.Invalidate();
        }
        else if(await GetSelectedEntry() is not null)
        {
            if(previewer is not null && !previewer.HasExited)
            {
                var oldPreviewer = previewer;
                previewer = null;
                _ = Task.Run(() =>
                {
                    oldPreviewer.Kill(true);
                    oldPreviewer.Dispose();
                });
            }

            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string previewerPath = Path.Combine(baseDir, "sunfire-kitty-previewer");
            string previewArgs = $"\"{(await GetSelectedEntry()).Value.Path}\" {SVRegistry.PreviewPane.SizeX} {SVRegistry.PreviewPane.SizeY} {SVRegistry.PreviewPane.OriginX} {SVRegistry.PreviewPane.OriginY}";
            await Logger.Debug(nameof(Sunfire), $"Previewing with args: \"{previewArgs}\"");

            previewer = new Process
            {
                StartInfo = new ProcessStartInfo()
                {
                    FileName = previewerPath,
                    Arguments = previewArgs,
                    UseShellExecute = false,
                    RedirectStandardOutput = false,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                }
            };
            
            previewer.Start();

            clean = false;
        }

    }

    private static async Task RefreshDirectoryHint()
    {
        SVRegistry.CurrentBorder.TitleLabel ??= new();
        SVRegistry.CurrentBorder.TitleLabel.Segments = [new() { Text = currentPath }];

        await SVRegistry.CurrentBorder.Invalidate();
    }

    private static async Task RefreshSelectionInfo(FSEntry? selectedEntry)
    {
        //Selection Full Path
        //Misc File Info
        if(selectedEntry is not null)
        {
            SVRegistry.BottomLeftBorder.TitleLabel ??= new();
            SVRegistry.BottomLeftBorder.TitleLabel.Segments = [new() { Text = selectedEntry.Value.Path }];
            
            if(selectedEntry.Value.IsDirectory)
                SVRegistry.BottomLeftLabel.Segments = [new() { Text = $" Directory {(await fsCache.GetEntries(selectedEntry.Value.Path, default)).Count}" }];
            else
            {
                var type = MediaRegistry.Scanner.Scan(selectedEntry.Value);
                
                SVRegistry.BottomLeftLabel.Segments = [new() { Text = $" File {selectedEntry.Value.Size}B (Type: \"{type}\")" }];
            }

        }
        else
        {
            SVRegistry.BottomLeftBorder.TitleLabel = null;
            SVRegistry.BottomLeftLabel.Segments = null;
        }

        await SVRegistry.BottomLeftBorder.Invalidate();
    }

    private static EntriesListView previewEntriesList = new();
    private static async Task<IRelativeSunfireView?> GetPreview(FSEntry? selectedEntry, CancellationToken token)
    {
        IRelativeSunfireView? view = null;
        if(selectedEntry is not null)
            if (selectedEntry.Value.IsDirectory)
            {
                await Logger.Debug(nameof(Sunfire), $"Previewing Directory \"{selectedEntry.Value.Path}\"");

                await previewEntriesList.UpdateCurrentPath(selectedEntry.Value.Path);

                view = previewEntriesList;
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
        await(await GetSelectedEntry() is var selectedEntry && selectedEntry is not null && selectedEntry.Value.IsDirectory 
            ? Refresh(selectedEntry.Value.Path) 
            : HandleFile());

    //Nav Helpers
    public static async Task NavList(int delta)
    {
        await SVRegistry.CurrentList.Nav(delta);  
        
        var previewToken = SecurePreviewGenToken();

        var selectedEntry = await GetSelectedEntry();

        try
        {
            var preview = await GetPreview(selectedEntry, previewToken);

            if(!previewToken.IsCancellationRequested)
                await Program.Renderer.EnqueueAction(async () =>
                {
                    await RefreshSelectionInfo(selectedEntry);
                    await RefreshPreview(preview);
                });
        }
        catch (OperationCanceledException) { }
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
}
