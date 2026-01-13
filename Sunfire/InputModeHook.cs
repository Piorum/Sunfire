using System.Text;
using Sunfire.Ansi.Models;
using Sunfire.Registries;
using Sunfire.Views;
using Sunfire.Views.Text;

namespace Sunfire;

public class InputModeHook()
{

    private InfoView? textDisplay;
    private string? _title;
    private char? _preCharacter;
    private readonly StringBuilder text = new();
    private Func<bool>? _warnSource;

    public async Task<string> EnableInputMode(string? title, char preCharacter, Func<bool> warnSource, Func<string, Task> onUpdate, List<(ConsoleKey key, Func<Task> task)> exitHandlers, List<(ConsoleKey key, Func<Task> task)> specialHandlers)
    {
        text.Clear();
        _title = title;
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

        await Program.InputHandler.EnableInputMode(
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

        return text.ToString();
    }

    private async Task AddTextDisplay()
    {
        var view = InfoView.New("", _title);
        textDisplay = view;

        await Program.Renderer.EnqueueAction(async () => 
        {
            SVRegistry.InfosView.SubViews.Add(textDisplay);
            await SVRegistry.RootPane.Invalidate();
        });

    }

    private async Task RemoveTextDisplay()
    {
        if(textDisplay is not null)
            await Program.Renderer.EnqueueAction(async () => 
            {
                SVRegistry.InfosView.SubViews.Remove(textDisplay);
                await SVRegistry.RootPane.Invalidate();
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
            textDisplay?.UpdateInfo(segments);
            await SVRegistry.InfosView.Invalidate();
        });
    }
}
