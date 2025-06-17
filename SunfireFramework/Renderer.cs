using System.Text;
using SunfireFramework.Rendering;
using SunfireFramework.Views;

namespace SunfireFramework;

public class Renderer(RootSV rootView)
{
    private SVBuffer _frontBuffer = new(rootView.SizeX, rootView.SizeY);
    private SVBuffer _backBuffer = new(rootView.SizeX, rootView.SizeY);

    private readonly RootSV _rootView = rootView;

    public async Task Render(CancellationToken token)
    {
        await _rootView.Arrange();
        
        while (!token.IsCancellationRequested)
        {
            await _rootView.Draw(_backBuffer);

            Console.ForegroundColor = ConsoleColor.White;
            Console.BackgroundColor = ConsoleColor.Black;
            if (!_frontBuffer.AsSpan().SequenceEqual(_backBuffer.AsSpan()))
            {
                (_backBuffer, _frontBuffer) = (_frontBuffer, _backBuffer);

                //Console.Write("test");
                StringBuilder sb = new();
                for (int y = 0; y < _rootView.SizeY; y++)
                {
                    for (int x = 0; x < _rootView.SizeX; x++)
                    {
                        sb.Append(_frontBuffer[x, y].Char);
                    }
                    sb.Append('\n');
                }
                Console.SetCursorPosition(0, 0);
                Console.Write(sb.ToString()[..^1]);
            }

            _backBuffer.Clear();
            
            try
            {
                await Task.Delay(6, token);
            }
            catch (OperationCanceledException) { }
        }
    }

}
