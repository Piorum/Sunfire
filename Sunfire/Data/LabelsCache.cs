using Sunfire.Ansi.Models;
using Sunfire.FSUtils.Models;
using Sunfire.Registries;
using Sunfire.Views.Text;

namespace Sunfire.Data;

public class LabelsCache : CacheBase<LabelsCache, LabelsCache.LabelContainer, FSEntry>, ICache<LabelsCache, LabelsCache.LabelContainer, FSEntry> 
{
    public class LabelContainer
    {
        required public FSEntry Key;
        required public LabelSVSlim View;
    }

    public static async Task<LabelContainer> FetchSingleAsync(FSEntry entry)
    {
        return await BuildLabel(entry);
    }

    public static async Task<IEnumerable<LabelContainer>> FetchMultipleAsync(IEnumerable<FSEntry> entries)
    {
        List<LabelContainer> labels = [];
        foreach(var entry in entries)
        {
            labels.Add(await BuildLabel(entry));
        }

        return labels;
    }

    public static Task<FSEntry> GetKey(LabelContainer label)
    {
        return Task.FromResult(label.Key);
    }

    
    private static readonly SStyle directoryStyle = new(ForegroundColor: ColorRegistry.DirectoryColor, Properties: SAnsiProperty.Bold);
    private static readonly SStyle fileStyle = new(ForegroundColor: ColorRegistry.FileColor);

    private static Task<LabelContainer> BuildLabel(FSEntry entry)
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
        
        LabelContainer container = new()
        {
            Key = entry,
            View = label
        };

        return Task.FromResult(container);
    }
}
