
using System.Threading.Channels;

namespace Sunfire.FSUtils.Models
{
    public abstract class FSEntry
    {
        public required string FullPath { get; init; }
        public required string Name { get; init; }
        public string Extension => Path.GetExtension(FullPath);
        public long Size { get; internal set; }
        public bool IsHidden { get; internal set; }
        public FSPermissions Permissions { get; internal set; } = new();
        public string Owner { get; internal set; } = string.Empty;
        public DateTime DateModified { get; internal set; }
        public FSDirectory? Parent { get; internal set; }

        internal ChannelWriter<Interfaces.IModificationAction> ActionQueue { get; init; }

        public async Task DeleteAsync()
        {
            var deleteAction = new Actions.DeleteAction(this);
            await ActionQueue.WriteAsync(deleteAction);
        }

        public async Task MoveAsync(string newPath)
        {
            var moveAction = new Actions.MoveAction(this, newPath);
            await ActionQueue.WriteAsync(moveAction);
        }
    }
}
