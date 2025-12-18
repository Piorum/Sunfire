using Sunfire.Ansi.Models;
using Sunfire.FSUtils.Models;
using Sunfire.Registries;
using Sunfire.Views.Text;

namespace Sunfire.Views;

public class EntryLabelView : LabelSVSlim
{
    private static readonly SStyle directoryStyle = new(ForegroundColor: ColorRegistry.DirectoryColor, Properties: SAnsiProperty.Bold);
    private static readonly SStyle fileStyle = new(ForegroundColor: ColorRegistry.FileColor);

    private FSEntry _entry;
    public FSEntry Entry 
    {
        get => _entry;
        set => (_entry, Dirty, built) = (value, true, false);
    }
    
    private bool built = false;

    override protected Task OnArrange()
    {
        if(built)
            return Task.CompletedTask;

        BuildSegments();
        return Task.CompletedTask;
    }

    private void BuildSegments()
    {
        SStyle style;

        if(Entry.IsDirectory)
            style = directoryStyle;
        else
            style = fileStyle;

        var segments = new LabelSegment[2]
        {
            new() { Text = " ", Style = style },
            new() { Text = Entry.Name, Style = style }
        };

        Segments = segments;

        built = true;
    }
}
