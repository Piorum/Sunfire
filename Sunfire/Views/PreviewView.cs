using System.Collections.Concurrent;
using Sunfire.Enums;
using Sunfire.FSUtils.Models;
using Sunfire.Previewers;
using Sunfire.Registries;
using Sunfire.Tui.Enums;
using Sunfire.Tui.Interfaces;
using Sunfire.Tui.Models;

namespace Sunfire.Views;

public class PreviewView : IRelativeSunfireView
{
    public int X { get; set; }
    public int Y { get; set; }
    public int Z { get; set; }

    public FillStyle FillStyleX { set; get; } = FillStyle.Max;
    public FillStyle FillStyleY { set; get; } = FillStyle.Max;
    public int StaticX { set; get; } = 1; //1 = 1 Cell
    public int StaticY { set; get; } = 1; //1 = 1 Cell
    public float PercentX { set; get; } = 1.0f; //1.0f == 100%
    public float PercentY { set; get; } = 1.0f; //1.0f == 100%

    public int OriginX { set; get; }
    public int OriginY { set; get; }
    public int SizeX { set; get; }
    public int SizeY { set; get; }

    public int MinX { get; } = 0;
    public int MinY { get; } = 0;

    private volatile bool Dirty;

    private readonly ConcurrentDictionary<MediaType, IPreviewer> previewers = [];
    public readonly DirectoryPreviewer directoryPreviewer = new();
    public readonly FallbackPreviewer fallbackPreviewer = new();

    private IPreviewer? activePreviewer = null;
    private IRelativeSunfireView? activeView = null;

    private readonly Lock gate = new();

    private FSEntry? currentEntry;

    public async Task Update(FSEntry? entry)
    {
        if(entry == currentEntry)
            return;
        currentEntry = entry;

        IPreviewer? next = entry is null ? null : SelectPreviewer(entry);
        IPreviewer? previous;

        bool previewerChanged;

        lock(gate)
        {
            previous = activePreviewer;
            previewerChanged = next != activePreviewer;
            activePreviewer = next;

            if (previewerChanged)
                activeView = null;
        }

        if (previewerChanged && previous is not null)
            await previous.CleanUp();

        if (next is null)
        {
            await Program.Renderer.EnqueueAction(async () => 
            {
                activeView = null;
                await Invalidate();
            });

            return;
        }

        var view = await next.Update(entry!.Value);

        lock(gate)
        {
            if (activePreviewer != next)
                return;

            activeView = view;
        }

        await Program.Renderer.EnqueueAction(Invalidate);
    }

    public void AddPreviewer(MediaType mediaType, IPreviewer previewer) =>
        previewers.TryAdd(mediaType, previewer);

    public Task<bool> Arrange()
    {
        if(Dirty)
        {
            var view = activeView;
            if(view is not null)
            {
                (view.OriginX, view.OriginY, view.SizeX, view.SizeY) = (OriginX, OriginY, SizeX, SizeY);
                view.Arrange();
            }

            Dirty = false;

            return Task.FromResult(true);
        }

        return Task.FromResult(false);

    }

    public async Task Draw(SVContext context)
    {
        var view = activeView;
        if(view is not null)
            await view.Draw(context);
    }

    public async Task Invalidate()
    {
        Dirty = true;

        var view = activeView;
        if(view is not null)
            await view.Invalidate();
    }

    private IPreviewer? SelectPreviewer(FSEntry? entry) =>
        entry is null
            ? null
            : entry.Value.IsDirectory
                ? directoryPreviewer
                : previewers.TryGetValue(MediaRegistry.GetMediaType(entry.Value), out var previewer)
                    ? previewer
                    : fallbackPreviewer;

    public interface IPreviewer
    {
        Task<IRelativeSunfireView?> Update(FSEntry entry);
        Task CleanUp();
    }    
}

