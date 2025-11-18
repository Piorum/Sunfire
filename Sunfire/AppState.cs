using System.Diagnostics;
using Sunfire.Registries;
using Sunfire.Views;

namespace Sunfire;

public static class AppState
{
    private static Dictionary<string, int> indexCache = [];

    private static string currentPath = "";
    private static string? CurrentEntry => SVRegistry.CurrentList.GetSelected()?.Text;

    public static async Task Init()
    {
        //Swap for finding directory program is opened in?
        var userProfleDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        if (!Directory.Exists(userProfleDir)) throw new("User profile is not a directory");
        currentPath = userProfleDir;
        indexCache.Add(currentPath,0);
        indexCache.Add("/home", 0);
        indexCache.Add("/", 4);

        await Program.Renderer.EnqueueAction(async () =>
        {
            SVRegistry.CurrentBorder.TitleLabel = new() { Text = currentPath };
            await SVRegistry.CurrentBorder.Invalidate();
        });

        await Refresh();
    }

    private static async Task Refresh()
    {
        await RefreshContainerList();
        await RefreshCurrentList();
        await RefreshPreview();
        await RefreshCurrentSelection();
    }

    private static async Task RefreshContainerList()
    {
        await SVRegistry.ContainerList.Clear();

        //Populate container list
        var directoryInfo = Directory.GetParent(currentPath);
        if (directoryInfo is null)
            return;

        var info = directoryInfo.GetFileSystemInfos()
            .OrderByDescending(e => Directory.Exists(e.FullName))
            .ThenByDescending(e => (e.Attributes & FileAttributes.Hidden) != 0)
            .ThenBy(e => e.Name.ToLowerInvariant());

        foreach (var entry in info)
        {
            if (Directory.Exists(entry.FullName))
            {
                await SVRegistry.ContainerList.AddLabel(new()
                {
                    TextProperties = Ansi.Models.SAnsiProperty.Bold,
                    Text = entry.Name
                });
            }
            else
            {
                await SVRegistry.ContainerList.AddLabel(new()
                {
                    Text = entry.Name
                });
            }
        }

        indexCache.TryGetValue(directoryInfo.FullName, out var previousIndex);
        if(previousIndex <= SVRegistry.ContainerList.MaxIndex && previousIndex > 0)
            SVRegistry.ContainerList.SelectedIndex = previousIndex;
        else
            indexCache[directoryInfo.FullName] = 0;

        await SVRegistry.ContainerList.Invalidate();
    }

    private static async Task RefreshCurrentList()
    {
        await SVRegistry.CurrentList.Clear();

        var directoryInfo = new DirectoryInfo(currentPath);
        var info = directoryInfo.GetFileSystemInfos()
            .OrderByDescending(e => Directory.Exists(e.FullName))
            .ThenByDescending(e => (e.Attributes & FileAttributes.Hidden) != 0)
            .ThenBy(e => e.Name.ToLowerInvariant());
        
        foreach (var entry in info)
        {
            if (Directory.Exists(entry.FullName))
            {
                await SVRegistry.CurrentList.AddLabel(new()
                {
                    TextProperties = Ansi.Models.SAnsiProperty.Bold,
                    Text = entry.Name
                });
            }
            else
            {
                await SVRegistry.CurrentList.AddLabel(new()
                {
                    Text = entry.Name
                });
            }
        }


        indexCache.TryGetValue(currentPath, out var previousIndex);
        if(previousIndex <= SVRegistry.CurrentList.MaxIndex && previousIndex > 0)
            SVRegistry.CurrentList.SelectedIndex = previousIndex;
        else
            indexCache[currentPath] = 0;

        await SVRegistry.CurrentList.Invalidate();
    }

    private static async Task RefreshPreview()
    {
        SVRegistry.PreviewPane.SubViews.Clear();

        if (CurrentEntry is not null)
        {
            var selectedEntryPath = Path.Combine(currentPath, CurrentEntry);

            if (Directory.Exists(selectedEntryPath))
            {
                ListSV previewList = new();        
                
                var directoryInfo = new DirectoryInfo(selectedEntryPath);
                var info = directoryInfo.GetFileSystemInfos()
                    .OrderByDescending(e => Directory.Exists(e.FullName))
                    .ThenByDescending(e => (e.Attributes & FileAttributes.Hidden) != 0)
                    .ThenBy(e => e.Name.ToLowerInvariant());
                
                foreach (var entry in info)
                {
                    if (Directory.Exists(entry.FullName))
                    {
                        await previewList.AddLabel(new()
                        {
                            TextProperties = Ansi.Models.SAnsiProperty.Bold,
                            Text = entry.Name
                        });
                    }
                    else
                    {
                        await previewList.AddLabel(new()
                        {
                            Text = entry.Name
                        });
                    }
                }

                indexCache.TryGetValue(directoryInfo.FullName, out var previousIndex);
                if(previousIndex <= previewList.MaxIndex && previousIndex > 0)
                    previewList.SelectedIndex = previousIndex;
                else
                    indexCache[directoryInfo.FullName] = 0;

                SVRegistry.PreviewPane.SubViews.Add(previewList);
            }
        }

        await SVRegistry.PreviewBorder.Invalidate();
    }

    private static async Task RefreshCurrentSelection()
    {
        var selectedEntryPath = Path.Combine(currentPath, CurrentEntry ?? "unknown");

        SVRegistry.BottomLeftBorder.TitleLabel ??= new();
        SVRegistry.BottomLeftBorder.TitleLabel.Text = selectedEntryPath;

        if (Directory.Exists(selectedEntryPath))
        {
            SVRegistry.BottomLeftLabel.Text = $" Directory {Directory.GetFiles(selectedEntryPath).Length + Directory.GetDirectories(selectedEntryPath).Length}";
        }
        else if (File.Exists(selectedEntryPath))
        {
            var fileInfo = new FileInfo(selectedEntryPath);
            SVRegistry.BottomLeftLabel.Text = $" Size: {fileInfo.Length}B";
        }

        await SVRegistry.BottomLeftBorder.Invalidate();
    }

    public static async Task NavUp()
    {
        if (SVRegistry.CurrentList.SelectedIndex > 0)
        {
            await Program.Renderer.EnqueueAction(async () =>
            {
                SVRegistry.CurrentList.SelectedIndex--;

                indexCache[currentPath] = SVRegistry.CurrentList.SelectedIndex;

                await SVRegistry.CurrentList.Invalidate();
                await RefreshCurrentSelection();
                await RefreshPreview();
            });
        }
    }
    public static async Task NavDown()
    {
        if (SVRegistry.CurrentList.SelectedIndex < SVRegistry.CurrentList.MaxIndex)
        {
            await Program.Renderer.EnqueueAction(async () =>
            {
                SVRegistry.CurrentList.SelectedIndex++;

                indexCache[currentPath] = SVRegistry.CurrentList.SelectedIndex;

                await SVRegistry.CurrentList.Invalidate();
                await RefreshCurrentSelection();
                await RefreshPreview();
            });
        }
    }
    public static async Task NavOut()
    {
        var directoryInfo = Directory.GetParent(currentPath);

        if (directoryInfo is not null)
        {
            currentPath = directoryInfo.FullName;
            await Program.Renderer.EnqueueAction(async () =>
            {
                SVRegistry.CurrentBorder.TitleLabel!.Text = currentPath;
                await SVRegistry.CurrentBorder.Invalidate();

                SVRegistry.ContainerList.SelectedIndex = 0;
                SVRegistry.CurrentList.SelectedIndex = 0;

                await Refresh();
            });
        }
    }
    public static async Task NavIn()
    {
        if (CurrentEntry is null)
            return;

        var selectedEntryPath = Path.Combine(currentPath, CurrentEntry);

        if (Directory.Exists(selectedEntryPath))
        {
            currentPath = selectedEntryPath;
            await Program.Renderer.EnqueueAction(async () =>
            {
                SVRegistry.CurrentBorder.TitleLabel!.Text = currentPath;
                await SVRegistry.CurrentBorder.Invalidate();

                SVRegistry.ContainerList.SelectedIndex = 0;
                SVRegistry.CurrentList.SelectedIndex = 0;
                await Refresh();
            });
        }
        else if (File.Exists(selectedEntryPath))
        {
            var psi = new ProcessStartInfo()
            {
                FileName = "xdg-open",
                Arguments = selectedEntryPath,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
            };
            Process.Start(psi);
        }
    }
}
