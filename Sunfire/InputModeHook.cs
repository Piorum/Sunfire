using System.Text;
using Sunfire.Ansi.Models;
using Sunfire.Registries;
using Sunfire.Views;
using Sunfire.Views.Text;

namespace Sunfire;

public class InputModeHook(PaneSV pane)
{
    private readonly PaneSV _pane = pane;

    private (BorderSV border, LabelSV label) textDisplay;
    private char? _preCharacter;
    private readonly StringBuilder text = new();
    private Func<bool>? _warnSource;

    public async Task<string> EnableInputMode(char preCharacter, Func<bool> warnSource, Func<string, Task> onUpdate, List<(ConsoleKey key, Func<Task> task)> exitHandlers, List<(ConsoleKey key, Func<Task> task)> specialHandlers)
    {
        text.Clear();
        _preCharacter = preCharacter;
        _warnSource = warnSource;
        TaskCompletionSource<string> tcs = new();

        await AddTextDisplay();
        await UpdateTextDisplay();

        async Task onExit()
        {
            await RemoveTextDisplay();
            tcs.TrySetResult(text.ToString());
        }

        List<(ConsoleKey key, Func<Task> task)> completeExitHandlers = [];
        foreach(var (key, task) in exitHandlers)
        {
            completeExitHandlers.Add((key, async () =>
            {
                await task();
                await onExit();
            }));
        }

        Program.InputHandler.EnableInputMode(
            textHandler: async (a) =>
            {
                text.Append(a);
                var textString = text.ToString();

                await onUpdate(textString);
                await UpdateTextDisplay();
            },
            deletionHandler: async () => 
            {
                if(text.Length > 0)
                    text.Remove(text.Length - 1, 1);
                var textString = text.ToString();

                await onUpdate(textString);
                await UpdateTextDisplay();
            },            
            exitHandlers: completeExitHandlers,
            specialHandlers
        );

        return await tcs.Task;
    }

    private async Task AddTextDisplay()
    {
        LabelSV label = new()
        {
            Y = 2
        };
        BorderSV border = new()
        {
            SubView = label
        };
        textDisplay = (border, label);

        await Program.Renderer.EnqueueAction(async () => 
        {
            _pane.SubViews.Add(border);
            await _pane.Invalidate();
        });

    }

    private async Task RemoveTextDisplay()
    {
        await Program.Renderer.EnqueueAction(async () => 
        {
            _pane.SubViews.Remove(textDisplay.border);
            await _pane.Invalidate();
        });
    }

    private async Task UpdateTextDisplay()
    {
        SStyle baseStyle = new() { ForegroundColor = _warnSource!() ? ColorRegistry.Red : null };

        var segments = new LabelSVSlim.LabelSegment[2]
        {
            new() { Text = $" {_preCharacter}{text}", Style = baseStyle },
            new() { Text = " ", Style = baseStyle with { Properties = SAnsiProperty.Underline } }
        };

        await Program.Renderer.EnqueueAction(async () =>
        {
            textDisplay.label.Segments = segments;
            await textDisplay.label.Invalidate();
        });
    }
}
