
using Sunfire.FSUtils.Interfaces;
using Sunfire.FSUtils.Models;

namespace Sunfire.FSUtils.Actions
{
    public class CreateDirectoryAction : IModificationAction
    {
        public string DirectoryPath { get; }
        public string Description => $"Create directory {DirectoryPath}";
        public FSEntry? Target => null;

        public CreateDirectoryAction(string directoryPath)
        {
            DirectoryPath = directoryPath;
        }

        public Task ExecuteAsync()
        {
            Directory.CreateDirectory(DirectoryPath);
            return Task.CompletedTask;
        }
    }
}
