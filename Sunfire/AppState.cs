using System.Diagnostics;
using Sunfire.Registries;
using Sunfire.Views;
using Sunfire.Views.Text;
using Sunfire.FSUtils;
using Sunfire.FSUtils.Models;
using Sunfire.Logging;
using Sunfire.Tui.Interfaces;
using System.Collections.Concurrent;
using Sunfire.Ansi.Models;
using System.Text;

namespace Sunfire;

public static class AppState
{
    public static readonly FSCache fsCache = new();
    private static readonly Dictionary<string, FSEntry?> selectedEntryCache = [];
    private static readonly ConcurrentDictionary<string, List<FSEntry>> sortedEntriesCache = [];
    private static readonly ConcurrentDictionary<string, List<LabelSVSlim>> builtLabelsCache = [];

    private static string currentPath = "";
    private static bool showHidden = false;

    private static readonly List<(FSEntry entry, LabelSVSlim label)> taggedEntries = [];

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
        ResetCache();

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

    public static async Task Reload()
    {
        fsCache.Clear();
        ResetCache();

        await Refresh();
    }

    private static void ResetCache()
    {
        sortedEntriesCache.Clear();
        builtLabelsCache.Clear();
    }

    private static async Task<FSEntry?> GetSelectedEntry() => 
        await SVRegistry.CurrentList.GetCurrentEntry();

    private static LabelSVSlim? GetSelectedLabel() => 
        SVRegistry.CurrentList.GetSelected();

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

    //Refresh Helpers
    private static async Task ClearAndInvalidateList(ListSV list)
    {
        await list.Clear();
        await list.Invalidate();
    }

    private static async Task UpdateList(ListSV list, (List<LabelSVSlim> newLabels, int previousSelectedIndex) newValues)
    {
        await list.Clear();

        await list.AddLabels(newValues.newLabels);
        list.SelectedIndex = newValues.previousSelectedIndex;

        await list.Invalidate();
    }

    private static async Task<(List<LabelSVSlim> newLabels, int previousSelectedIndex)> GetLabelsAndIndex(string path, CancellationToken token = default)
    {
        var entriesStartTime = DateTime.Now;

        var entries = await GetEntries(path, token);

        await Logger.Debug(nameof(Sunfire), $"Get {path} Entries {(DateTime.Now - entriesStartTime).TotalMicroseconds}us");

        if(!builtLabelsCache.TryGetValue(path, out var labels))
        {

            var labelsStartTime = DateTime.Now;

            labels = new List<LabelSVSlim>(entries.Count);

            SStyle directoryStyle = new(ForegroundColor: ColorRegistry.DirectoryColor, Properties: SAnsiProperty.Bold);
            SStyle fileStyle = new(ForegroundColor: ColorRegistry.FileColor);

            HashSet<string>? taggedPaths = taggedEntries.Count > 0 
                ? [.. taggedEntries.Select(e => e.entry.Path)]
                : null;
              
            foreach (var entry in entries)
            {
                SStyle style;

                if(entry.IsDirectory)
                    style = directoryStyle;
                else
                    style = fileStyle;

                var segments = new LabelSVSlim.LabelSegment[1]
                {
                    new() { Text = entry.Name, Style = style }
                };

                LabelSVSlim label = new() { Segments = segments };

                if(taggedPaths is not null && taggedPaths.Contains(entry.Path))
                {
                    label.LabelProperties |= Views.Enums.LabelSVProperty.Tagged;
                    taggedEntries.RemoveAll(e => e.entry.Path == entry.Path);
                    taggedEntries.Add((entry, label));
                }
                
                labels.Add(label);
            }

            builtLabelsCache.TryAdd(path, labels);

            await Logger.Debug(nameof(Sunfire), $"Build {path} Labels {(DateTime.Now - labelsStartTime).TotalMicroseconds}us");
        }

        var previousSelectedIndex = GetPreviousIndex(entries, path);

        return (labels, previousSelectedIndex);
    }

    private static async Task<List<FSEntry>> GetEntries(string path, CancellationToken token = default)
    {
        if(sortedEntriesCache.TryGetValue(path, out var cachedEntries))
            return cachedEntries;
        
        List<FSEntry> newEntries = [.. OrderAndFilterEntries(await fsCache.GetEntries(path, token))];
        sortedEntriesCache.TryAdd(path, newEntries);

        return newEntries;
    }

    private static IOrderedEnumerable<FSEntry> OrderAndFilterEntries(IEnumerable<FSEntry> entries)
    {
        if(!showHidden)
            entries = entries.Where(e => !e.Attributes.HasFlag(FileAttributes.Hidden));

        return entries
            .OrderByDescending(e => e.IsDirectory)
            .ThenByDescending(e => e.Attributes.HasFlag(FileAttributes.Hidden))
            .ThenBy(e => e.Name.ToLowerInvariant());
    }

    private static int GetPreviousIndex(List<FSEntry> entries, string path)
    {
        selectedEntryCache.TryGetValue(path, out var previousEntry);

        if(previousEntry is null)
            return 0;

        return entries.IndexOf((FSEntry)previousEntry) is var index && index >= 0 
            ? index 
            : 0; //If not found in entries use 0
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

    public static async Task ToggleTagOnSelectedEntry()
    {
        var currentEntry = await GetSelectedEntry();
        var currentLabel = GetSelectedLabel();

        if(currentEntry is null || currentLabel is null)
            return;

        currentLabel.LabelProperties ^= Views.Enums.LabelSVProperty.Tagged;
        taggedEntries.Add((currentEntry.Value, currentLabel));

        await Logger.Debug(nameof(Sunfire), $"Tagged {currentEntry.Value.Path}");

        if(SVRegistry.CurrentList.MaxIndex != SVRegistry.CurrentList.SelectedIndex)
            await NavDown();
        else
            await Program.Renderer.EnqueueAction(SVRegistry.CurrentList.Invalidate);
    }

    public static async Task ClearTags()
    {
        taggedEntries.Clear();

        await Reload();
    }

    public static async Task Search()
    {
        var channelReader = await Program.InputHandler.EnableInputMode();

        StringBuilder sb = new();
        LabelSVSlim label = SVRegistry.BottomLeftLabel;

        await Program.Renderer.EnqueueAction(() =>
        {
            label.Segments = [new() { Text = $" /", Style = new() }, new() { Text = " ", Style = new( ForegroundColor: ColorRegistry.FileColor, Properties: SAnsiProperty.Underline ) }];
            return Task.CompletedTask;
        });
        
        var currentEntries = await GetEntries(currentPath);
        var initialIndex = SVRegistry.CurrentList.SelectedIndex;

        await foreach(var input in channelReader.ReadAllAsync())
        {
            //Ignore mouse input for search
            if(input.Key.InputType != Input.Enums.InputType.Keyboard)
                continue;

            //Restore orignal selection
            if (input.Key.KeyboardKey == ConsoleKey.Escape)
            {
                var initialIndexOffset = SVRegistry.CurrentList.SelectedIndex - initialIndex;
                if(initialIndexOffset != 0)
                    await NavList(-initialIndexOffset);

                break;
            }
            //Stop getting input and leave new selection
            else if (input.Key.KeyboardKey == ConsoleKey.Enter || input.Key.KeyboardKey == ConsoleKey.Tab)
                break;

            //Handle normal input
            if(input.Key.KeyboardKey == ConsoleKey.Backspace)
            {
                if(sb.Length > 0)
                    sb.Remove(sb.Length - 1, 1);
            }
            else if(input.InputData.UTFChar != default)
            {
                sb.Append(input.InputData.UTFChar);
            }
            
            string search = sb.ToString();
            bool isSearchEmpty = search.Length == 0;

            var bestMatch = GetBestMatch(currentEntries, search);
            bool matchFound = bestMatch is not null;

            if(matchFound)
            {
                var newSelectedIndex = currentEntries.IndexOf(bestMatch!.Value);
                int offset = SVRegistry.CurrentList.SelectedIndex - newSelectedIndex;

                if(offset != 0)
                {
                    await NavList(-offset);
                    await Logger.Debug(nameof(Sunfire), $"Searched \"{search}\" with result \"{bestMatch.Value.Name}\"");
                }
            }

            SColor? searchTextColor = (!matchFound && !isSearchEmpty) ? ColorRegistry.Red : null;

            await Program.Renderer.EnqueueAction(() =>
            {
                label.Segments = [new() { Text = $" /{sb}", Style = new( ForegroundColor: searchTextColor ) }, new() { Text = " ", Style = new( ForegroundColor: searchTextColor, Properties: SAnsiProperty.Underline )}]; 
                return Task.CompletedTask;
            });
        }

        await Program.InputHandler.DisableInputMode();
        await Program.Renderer.EnqueueAction(async () => await RefreshSelectionInfo(await GetSelectedEntry()));
    }

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


    public static async Task Command()
    {
        var channelReader = await Program.InputHandler.EnableInputMode();

        StringBuilder sb = new();
        LabelSVSlim label = SVRegistry.BottomLeftLabel;

        await Program.Renderer.EnqueueAction(() =>
        {
            label.Segments = [new() { Text = $" :", Style = new() }, new() { Text = " ", Style = new( ForegroundColor: ColorRegistry.FileColor, Properties: SAnsiProperty.Underline ) }];
            return Task.CompletedTask;
        });

        await foreach(var input in channelReader.ReadAllAsync())
        {
            //Ignore mouse input for search
            if(input.Key.InputType != Input.Enums.InputType.Keyboard)
                continue;

            //Restore orignal selection
            if (input.Key.KeyboardKey == ConsoleKey.Escape)
            {
                await Program.InputHandler.DisableInputMode();
                await Program.Renderer.EnqueueAction(async () => await RefreshSelectionInfo(await GetSelectedEntry()));
                return;
            }

            //Stop getting input and leave new selection
            else if (input.Key.KeyboardKey == ConsoleKey.Enter || input.Key.KeyboardKey == ConsoleKey.Tab)
                break;

            //Handle normal input
            if(input.Key.KeyboardKey == ConsoleKey.Backspace)
            {
                if(sb.Length > 0)
                    sb.Remove(sb.Length - 1, 1);
            }
            else if(input.InputData.UTFChar != default)
            {
                sb.Append(input.InputData.UTFChar);
            }

            await Program.Renderer.EnqueueAction(() =>
            {
                label.Segments = [new() { Text = $" :{sb}", Style = new( ForegroundColor: ColorRegistry.FileColor ) }, new() { Text = " ", Style = new( ForegroundColor: ColorRegistry.FileColor, Properties: SAnsiProperty.Underline )}]; 
                return Task.CompletedTask;
            });
        }

        var task = sb.ToString().ToLower() switch
        {
            "copy" => Copy(),
            "cut" => Cut(),
            "delete" => Delete(),
            _ => Task.CompletedTask
        };

        await task;

        await Program.InputHandler.DisableInputMode();
        await Program.Renderer.EnqueueAction(async () => await RefreshSelectionInfo(await GetSelectedEntry()));
    }

    public static async Task Copy()
    {
        var channelReader = await Program.InputHandler.EnableInputMode();

        StringBuilder sb = new();
        LabelSVSlim label = SVRegistry.BottomLeftLabel;

        await Program.Renderer.EnqueueAction(() =>
        {
            label.Segments = [new() { Text = $" Copy {taggedEntries.Count} Entries? (Y/N)", Style = new() }];
            return Task.CompletedTask;
        });
    
        await foreach(var input in channelReader.ReadAllAsync())
        {
            if (input.Key.KeyboardKey == ConsoleKey.Y)
            {
                var source = GetTaggedPaths();
                var dest = currentPath;

                if(!string.IsNullOrEmpty(source) && !string.IsNullOrEmpty(dest) && Directory.Exists(dest))
                {
                    await Logger.Info(nameof(Sunfire), $"Copying {source} to {dest}");

                    Process.Start(
                        new ProcessStartInfo()
                        {
                            FileName = "cp",
                            Arguments = $"-n -r {source} \"{dest}\"",
                            UseShellExecute = false,
                            RedirectStandardOutput = true,
                            RedirectStandardError = true,
                            CreateNoWindow = true,
                        }
                    );
                }
                else
                    await Logger.Info(nameof(Sunfire), $"Bad copy tried \"{source} to {dest}\"");

                await Program.InputHandler.DisableInputMode();
                await Reload();
                
                break;
            }
            else
            {
                await Program.InputHandler.DisableInputMode();
                await Program.Renderer.EnqueueAction(async () => await RefreshSelectionInfo(await GetSelectedEntry()));
                
                break;
            }
        }

        await Program.InputHandler.DisableInputMode();
        await Program.Renderer.EnqueueAction(async () => await RefreshSelectionInfo(await GetSelectedEntry()));
    }

    public static async Task Cut()
    {
        var channelReader = await Program.InputHandler.EnableInputMode();

        StringBuilder sb = new();
        LabelSVSlim label = SVRegistry.BottomLeftLabel;

        await Program.Renderer.EnqueueAction(() =>
        {
            label.Segments = [new() { Text = $" Cut {taggedEntries.Count} Entries? (Y/N)", Style = new() }];
            return Task.CompletedTask;
        });
    
        await foreach(var input in channelReader.ReadAllAsync())
        {
            if (input.Key.KeyboardKey == ConsoleKey.Y)
            {
                var source = GetTaggedPaths();
                var dest = currentPath;

                if(!string.IsNullOrEmpty(source) && !string.IsNullOrEmpty(dest) && Directory.Exists(dest))
                {
                    await Logger.Info(nameof(Sunfire), $"Cutting {source} to {dest}");

                    Process.Start(
                        new ProcessStartInfo()
                        {
                            FileName = "mv",
                            Arguments = $"-n {source} \"{dest}\"",
                            UseShellExecute = false,
                            RedirectStandardOutput = true,
                            RedirectStandardError = true,
                            CreateNoWindow = true,
                        }
                    );
                }
                else
                    await Logger.Info(nameof(Sunfire), $"Bad cut tried \"{source} to {dest}\"");

                await Program.InputHandler.DisableInputMode();
                await Reload();
                
                break;
            }
            else
            {
                await Program.InputHandler.DisableInputMode();
                await Program.Renderer.EnqueueAction(async () => await RefreshSelectionInfo(await GetSelectedEntry()));
                
                break;
            }
        }

        await Program.InputHandler.DisableInputMode();
        await Program.Renderer.EnqueueAction(async () => await RefreshSelectionInfo(await GetSelectedEntry()));
    }

    public static async Task Delete()
    {
        string source;

        if(taggedEntries.Count > 0)
            source = GetTaggedPaths();
        else
            source = $"\"{(await GetSelectedEntry()).Value.Path}\"";

        var channelReader = await Program.InputHandler.EnableInputMode();

        StringBuilder sb = new();
        LabelSVSlim label = SVRegistry.BottomLeftLabel;

        await Program.Renderer.EnqueueAction(() =>
        {
            label.Segments = [new() { Text = $" Remove {(taggedEntries.Count > 0 ? $"{taggedEntries.Count} Entries" : source)}? (Y/N)", Style = new() }];
            return Task.CompletedTask;
        });

        await foreach(var input in channelReader.ReadAllAsync())
        {
            if (input.Key.KeyboardKey == ConsoleKey.Y)
            {
                await Logger.Info(nameof(Sunfire), $"Removing {source}");

                Process.Start(
                    new ProcessStartInfo()
                    {
                        FileName = "rm",
                        Arguments = $"-r {source}",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true,
                    }
                );

                await Program.InputHandler.DisableInputMode();
                await ClearTags();
                await Reload();

                break;
            }
            else
            {
                await Program.InputHandler.DisableInputMode();
                await Program.Renderer.EnqueueAction(async () => await RefreshSelectionInfo(await GetSelectedEntry()));

                break;
            }
        }
    }

    public static string GetTaggedPaths() =>
        string.Join(' ', taggedEntries.Where(e => File.Exists(e.entry.Path) || Directory.Exists(e.entry.Path)).Select(e => $"\"{e.entry.Path}\""));

    public static async Task Bash()
    {
        var channelReader = await Program.InputHandler.EnableInputMode();

        StringBuilder sb = new();
        LabelSVSlim label = SVRegistry.BottomLeftLabel;

        await Program.Renderer.EnqueueAction(() =>
        {
            label.Segments = [new() { Text = $" $", Style = new() }, new() { Text = " ", Style = new( ForegroundColor: ColorRegistry.FileColor, Properties: SAnsiProperty.Underline ) }];
            return Task.CompletedTask;
        });

        await foreach(var input in channelReader.ReadAllAsync())
        {
            //Ignore mouse input for search
            if(input.Key.InputType != Input.Enums.InputType.Keyboard)
                continue;

            //Restore orignal selection
            if (input.Key.KeyboardKey == ConsoleKey.Escape || input.Key.KeyboardKey == ConsoleKey.Tab)
            {
                await Program.InputHandler.DisableInputMode();
                await Program.Renderer.EnqueueAction(async () => await RefreshSelectionInfo(await GetSelectedEntry()));
                return;
            }

            //Stop getting input and leave new selection
            else if (input.Key.KeyboardKey == ConsoleKey.Enter)
                break;

            //Handle normal input
            if(input.Key.KeyboardKey == ConsoleKey.Backspace)
            {
                if(sb.Length > 0)
                    sb.Remove(sb.Length - 1, 1);
            }
            else if(input.InputData.UTFChar != default)
            {
                sb.Append(input.InputData.UTFChar);
            }

            await Program.Renderer.EnqueueAction(() =>
            {
                label.Segments = [new() { Text = $" ${sb}", Style = new( ForegroundColor: ColorRegistry.FileColor ) }, new() { Text = " ", Style = new( ForegroundColor: ColorRegistry.FileColor, Properties: SAnsiProperty.Underline )}]; 
                return Task.CompletedTask;
            });
        }

        string command = sb.ToString();

        await Logger.Info(nameof(Sunfire), $"Executing bash \"{command}\"");

        Process.Start(
            new ProcessStartInfo()
            {
                FileName = "bash",
                Arguments = $"-c \"{command}\"",
                WorkingDirectory = currentPath,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
            }
        );

        await Program.InputHandler.DisableInputMode();
        await Reload();
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
