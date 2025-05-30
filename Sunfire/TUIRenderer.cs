using Sunfire.Enums;
using Sunfire.Views;

namespace Sunfire;

public static class TUIRenderer
{
    private static readonly SemaphoreSlim _renderLock = new(1);

    public static Task ExecuteRenderTasks(View rootView)
    {
        return Task.CompletedTask;
    }

    public static async Task ExecuteRenderAction(View rootView, RenderAction renderAction)
    {
        await _renderLock.WaitAsync();

        var task = renderAction switch
        {
            RenderAction.Arrange => Task.Run(async () =>
            {
                Console.Clear();
                await rootView.Arrange(Console.BufferWidth, Console.BufferHeight);
            }),
            //RenderAction.FullRedraw => rootView.Draw(),
            _ => throw new NotImplementedException("Render action had no specified case.")
        };
        await task;

        _renderLock.Release();
    }
}
