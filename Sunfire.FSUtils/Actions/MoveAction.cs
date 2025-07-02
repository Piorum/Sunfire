using Sunfire.FSUtils.Enums;
using Sunfire.FSUtils.Interfaces;
using Sunfire.FSUtils.Models;

namespace Sunfire.FSUtils.Actions;

public class MoveAction(FSEntry target, string newPath) : IModificationAction
{
    public string Description => $"Move {Target.FullPath} to {NewPath}";
    public FSEntry Target { get; } = target;
    public ActionProperty ActionProperties { get; } = ActionProperty.Destructive;

    public string NewPath { get; } = newPath;

    public Task ExecuteAsync()
    {
        if (Target is FSDirectory)
        {
            Directory.Move(Target.FullPath, NewPath);
        }
        else
        {
            File.Move(Target.FullPath, NewPath);
        }
        return Task.CompletedTask;
    }
}
