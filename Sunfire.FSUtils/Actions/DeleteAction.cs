
using Sunfire.FSUtils.Interfaces;
using Sunfire.FSUtils.Models;

namespace Sunfire.FSUtils.Actions
{
    public class DeleteAction : IModificationAction
    {
        public FSEntry Target { get; }
        public string Description => $"Permanently delete {Target.FullPath}";

        public DeleteAction(FSEntry target)
        {
            Target = target;
        }

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
}
