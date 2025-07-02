using Sunfire.FSUtils.Enums;
using Sunfire.FSUtils.Interfaces;
using Sunfire.FSUtils.Models;

namespace Sunfire.FSUtils.Actions;

public class DeleteAction(FSEntry target) : IModificationAction
{
    public string Description => $"Permanently delete {Target.FullPath}";
    public FSEntry Target { get; } = target;
    public ActionProperty ActionProperties { get; } = ActionProperty.Destructive;

    public Task ExecuteAsync()
    {
        if (Target is FSDirectory)
        {
            Directory.Delete(Target.FullPath, recursive: true);
        }
        else
        {
            File.Delete(Target.FullPath);
        }
        return Task.CompletedTask;
    }
}
