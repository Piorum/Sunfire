namespace Sunfire.Input.Types;

public readonly record struct Bind
{
    public Guid Id { get; }
    public Func<InputData, Task> Task { get; }

    public Bind(Func<InputData, Task> task)
    {
        Id = Guid.NewGuid();
        Task = task;
    }
}
