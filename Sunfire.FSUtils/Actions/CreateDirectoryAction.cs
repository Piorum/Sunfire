using Sunfire.FSUtils.Enums;
using Sunfire.FSUtils.Interfaces;
using Sunfire.FSUtils.Models;

namespace Sunfire.FSUtils.Actions;

public class CreateDirectoryAction(string directoryPath) : IModificationAction
{
    public string Description => $"Create directory {DirectoryPath}";
    public FSEntry? Target => null;
    public ActionProperty ActionProperties { get; } = ActionProperty.Destructive;

    public string DirectoryPath { get; } = directoryPath;

    public Task ExecuteAsync()
    {
        Directory.CreateDirectory(DirectoryPath);
        return Task.CompletedTask;
    }
}
