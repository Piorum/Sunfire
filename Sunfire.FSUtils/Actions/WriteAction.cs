
using Sunfire.FSUtils.Interfaces;
using Sunfire.FSUtils.Models;

namespace Sunfire.FSUtils.Actions
{
    public class WriteAction : IModificationAction
    {
        public string FilePath { get; }
        public byte[] Content { get; }
        public string Description => $"Write {Content.Length} bytes to {FilePath}";
        public FSEntry? Target => null; // This action creates a file, so there's no existing target

        public WriteAction(string filePath, byte[] content)
        {
            FilePath = filePath;
            Content = content;
        }

        public async Task ExecuteAsync()
        {
            await File.WriteAllBytesAsync(FilePath, Content);
        }
    }
}
