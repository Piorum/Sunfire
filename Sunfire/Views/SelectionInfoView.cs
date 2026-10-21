using Sunfire.FSUtils;
using Sunfire.FSUtils.Models;
using Sunfire.Registries;
using Sunfire.Views.Text;
using Wrath.Views;

namespace Sunfire.Views;

public class SelectionInfoView : Border
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

    public async void Update(FSEntry? entry)
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
                    var entries = FSCache.GetEntries(path);

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

            Program.App.Renderer.EnqueueAction(Invalidate);
        }
    }

    override protected void OnArrange()
    {
        //TitleLabel = titleLabel;
        label.Segments = labelSegments;

        base.OnArrange();
    }    
    
    private CancellationToken SecureSelectionInfoGenToken()
    {
        selectionInfoGenCts?.Cancel();
        selectionInfoGenCts?.Dispose();
        
        selectionInfoGenCts = new();
        return selectionInfoGenCts.Token;
    }

}
