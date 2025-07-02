
namespace Sunfire.FSUtils.Models
{
    public class FSFile : FSEntry
    {
        public async Task<byte[]> ReadBytesAsync()
        {
            return await File.ReadAllBytesAsync(FullPath);
        }

        public async Task<string> ReadTextAsync()
        {
            return await File.ReadAllTextAsync(FullPath);
        }

        public async Task WriteBytesAsync(byte[] content)
        {
            var writeAction = new Actions.WriteAction(FullPath, content);
            await ActionQueue.WriteAsync(writeAction);
        }

        public async Task WriteTextAsync(string content)
        {
            var writeAction = new Actions.WriteAction(FullPath, System.Text.Encoding.UTF8.GetBytes(content));
            await ActionQueue.WriteAsync(writeAction);
        }
    }
}
