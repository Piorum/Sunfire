
using Sunfire.FSUtils.Interfaces;
using Sunfire.FSUtils.Models;

namespace Sunfire.FSUtils.Actions
{
    public class MoveAction : IModificationAction
    {
        public FSEntry Target { get; }
        public string NewPath { get; }
        public string Description => $"Move {Target.FullPath} to {NewPath}";

        FSEntry? IModificationAction.Target => Target;

        public MoveAction(FSEntry target, string newPath)
        {
            Target = target;
            NewPath = newPath;
        }

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
}
