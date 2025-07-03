using System.Threading.Channels;

namespace Sunfire.FSUtils.Models;

public abstract class FSEntry
{
    public required string FullPath { get; init; }
    public required string Name { get; init; }
    public string Extension => Path.GetExtension(FullPath);
    public long Size { get; internal set; }
    public bool IsHidden { get; internal set; }
    public DateTime DateModified { get; internal set; }
    public FSDirectory? Parent { get; internal set; }

    required public ChannelWriter<Interfaces.IModificationAction> ActionQueue { get; init; }

    
}
