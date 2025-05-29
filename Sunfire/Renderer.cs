using Sunfire.Enums;
using Sunfire.Views;

namespace Sunfire;

public static class Renderer
{
    public static Task ExecuteRenderTasks(View rootView)
    {
        return Task.CompletedTask;
    }

    public static async Task ExecuteRenderAction(View rootView, RenderAction renderAction)
    {
        var task = renderAction switch
        {
            RenderAction.Arrange => rootView.Arrange(Console.BufferWidth, Console.BufferHeight),
            RenderAction.FullRedraw => rootView.Draw(),
            _ => throw new NotImplementedException("Render action had no specified case.")
        };

        await task;
    }
}
