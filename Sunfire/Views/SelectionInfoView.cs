using Sunfire.FSUtils;
using Sunfire.FSUtils.Models;
using Sunfire.Registries;
using Sunfire.Views.Text;

namespace Sunfire.Views;

public class SelectionInfoView : BorderSV
{
    private readonly LabelSV label;

    private LabelSVSlim? titleLabel = null;
    private LabelSVSlim.LabelSegment[]? labelSegments = null;

    private CancellationTokenSource? selectionInfoGenCts;

    public SelectionInfoView(LabelSV subLabel)
    {
        label = subLabel;
        SubView = subLabel;
    }

    public async Task Update(FSEntry? entry)
    {
        var token = SecureSelectionInfoGenToken();

        LabelSVSlim? tmpTitleLabel = null;
        LabelSVSlim.LabelSegment[]? subLabelSegments = null;
        if(entry is not null)
        {
            var path = entry.Value.Path;

            tmpTitleLabel = new()
            {
                Segments = [new() { Text = path }]
            };

            if(entry.Value.IsDirectory)
            {
                try
                {
                    var entries = await FSCache.GetEntries(entry.Value.Directory, token);

                    subLabelSegments = [new() { Text = $" Directory {entries.Count}" }];
                }
                catch (OperationCanceledException) { }
            }
            else
            {
                var type = MediaRegistry.Scanner.Scan(entry.Value);
                
                subLabelSegments = [new() { Text = $" File {entry.Value.Size}B (Type: \"{type}\")" }];
            }
        }

        if(!token.IsCancellationRequested)
        {
            titleLabel = tmpTitleLabel;
            labelSegments = subLabelSegments;

            await Program.Renderer.EnqueueAction(Invalidate);
        }
    }

    override protected async Task OnArrange()
    {
        TitleLabel = titleLabel;
        label.Segments = labelSegments;

        await base.OnArrange();
    }    
    
    private CancellationToken SecureSelectionInfoGenToken()
    {
        selectionInfoGenCts?.Cancel();
        selectionInfoGenCts?.Dispose();
        
        selectionInfoGenCts = new();
        return selectionInfoGenCts.Token;
    }

}
