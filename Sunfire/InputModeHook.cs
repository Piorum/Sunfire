using System.Text;
using Moonfire.Ansi.Models;
using Moonfire.Input.Models;
using Sunfire.Registries;
using Sunfire.Views;
using Sunfire.Views.Text;

namespace Sunfire;

public class InputModeHook
{
    private InfoView? _textDisplay;
    private readonly StringBuilder _text = new();

    public void Prompt(
        string title,
        char preCharacter,
        Action<string>? onSubmit = null,
        Action? onCancel = null,
        Action<string>? onUpdate = null,
        Func<bool>? warnSource = null,
        List<(ConsoleKey key, Action action)>? specialHandlers = null,
        CancellationToken token = default)
    {
        _text.Clear();

        AddTextDisplay(title);
        UpdateTextDisplay(preCharacter, warnSource);

        // 1. Take over input. 'using' guarantees it is closed even on unexpected exit.
        using var rawInput = Program.App.InputHandler.OpenRaw();

        try
        {
            while (!token.IsCancellationRequested)
            {
                TerminalInput input;
                try
                {
                    input = rawInput.Take(token);
                }
                catch (OperationCanceledException)
                {
                    break;
                }

                var key = input.Key.KeyboardKey;

                // Escape = Cancel
                if (key == ConsoleKey.Escape)
                {
                    onCancel?.Invoke();
                    break;
                }

                // Enter or Tab = Submit
                if (key is ConsoleKey.Enter or ConsoleKey.Tab)
                {
                    onSubmit?.Invoke(_text.ToString());
                    break;
                }

                // Special handlers (e.g. Ctrl+U, custom keybinds inside input mode)
                if (specialHandlers is not null)
                {
                    bool handled = false;
                    for (int i = 0; i < specialHandlers.Count; i++)
                    {
                        if (specialHandlers[i].key == key)
                        {
                            specialHandlers[i].action();
                            UpdateTextDisplay(preCharacter, warnSource);
                            handled = true;
                            break;
                        }
                    }
                    if (handled) continue;
                }

                // Backspace = Delete character
                if (key == ConsoleKey.Backspace)
                {
                    if (_text.Length > 0)
                    {
                        _text.Remove(_text.Length - 1, 1);
                        var current = _text.ToString();
                        onUpdate?.Invoke(current);
                        UpdateTextDisplay(preCharacter, warnSource);
                    }
                    continue;
                }

                // Printable character input
                if (input.InputData.UTFChar is { } c && !char.IsControl(c))
                {
                    _text.Append(c);
                    var current = _text.ToString();
                    onUpdate?.Invoke(current);
                    UpdateTextDisplay(preCharacter, warnSource);
                }
            }
        }
        finally
        {
            // 2. ALWAYS remove the visual display, even if an exception or cancellation occurs!
            RemoveTextDisplay();
        }
    }

    private void AddTextDisplay(string title)
    {
        _textDisplay = InfoView.New("", title);
        Program.App.Renderer.EnqueueAction(() =>
        {
            SVRegistry.InfosView.SubViews.Add(_textDisplay);
            SVRegistry.RootPane.Invalidate();
        });
    }

    private void RemoveTextDisplay()
    {
        if (_textDisplay is not null)
        {
            var displayToRemove = _textDisplay;
            _textDisplay = null;

            Program.App.Renderer.EnqueueAction(() =>
            {
                SVRegistry.InfosView.SubViews.Remove(displayToRemove);
                SVRegistry.RootPane.Invalidate();
            });
        }
    }

    private void UpdateTextDisplay(char preCharacter, Func<bool>? warnSource)
    {
        bool isWarning = warnSource?.Invoke() ?? false;
        AnsiStyleData baseStyle = new() { ForegroundColor = isWarning ? ColorRegistry.Red : null };

        var segments = new LabelSVSlim.LabelSegment[2]
        {
            new() { Text = $" {preCharacter}{_text}", Style = baseStyle },
            new() { Text = " ", Style = baseStyle with { Properties = AnsiProperty.Underline } }
        };

        Program.App.Renderer.EnqueueAction(() =>
        {
            _textDisplay?.UpdateInfo(segments);
            SVRegistry.InfosView.Invalidate();
        });
    }
}