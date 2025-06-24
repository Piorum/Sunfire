using System.Text;
using Sunfire.Ansi.Models;
using Sunfire.Ansi.Registries;

namespace Sunfire.Ansi;

public class AnsiStringBuilder()
{
    private readonly StringBuilder sb = new();
    private SStyle currentState = default;

    private readonly static (SAnsiProperty, string, string)[] modifierActionsLookup =
    [
        (SAnsiProperty.Bold, AnsiRegistry.Bold, AnsiRegistry.DisableBold),
        (SAnsiProperty.Italic, AnsiRegistry.Italic, AnsiRegistry.DisableItalic),
        (SAnsiProperty.Underline, AnsiRegistry.Underline, AnsiRegistry.DisableUnderline),
        (SAnsiProperty.Highlight, AnsiRegistry.ReverseVideoMode, AnsiRegistry.DisableReverseVideoMode),
        (SAnsiProperty.Strikethrough, AnsiRegistry.Strikethrough, AnsiRegistry.DisableStrikethrough)
    ];
    
    public AnsiStringBuilder Append(string text, SStyle desiredState)
    {
        UpdateStyle(desiredState);
        if (!string.IsNullOrEmpty(text))
            sb.Append(text);

        return this;
    }

    private void UpdateStyle(SStyle desiredState)
    {
        if (desiredState.CursorPosition is { } pos)
            sb.Append(AnsiRegistry.MoveCursor(pos.Y, pos.X));

        if (currentState.ForegroundColor != desiredState.ForegroundColor)
            sb.Append(AnsiRegistry.SetForegroundColor(desiredState.ForegroundColor));
        if (currentState.BackgroundColor != desiredState.BackgroundColor)
            sb.Append(AnsiRegistry.SetBackgroundColor(desiredState.BackgroundColor));

        var removedProperties = currentState.Properties & ~desiredState.Properties;
        var addedProperties = desiredState.Properties & ~currentState.Properties;

        foreach (var (modifier, onCode, offCode) in modifierActionsLookup)
            if (addedProperties.HasFlag(modifier))
                sb.Append(onCode);
            else if (removedProperties.HasFlag(modifier))
                sb.Append(offCode);

        currentState = desiredState with { CursorPosition = null };
    }

    public AnsiStringBuilder ShowCursor()
    {
        sb.Append(AnsiRegistry.ShowCursor);
        return this;
    }
    public AnsiStringBuilder HideCursor()
    {
        sb.Append(AnsiRegistry.HideCursor);
        return this;
    }

    public AnsiStringBuilder ResetProperties()
    {
        sb.Append(AnsiRegistry.ResetProperties);
        return this;
    }
    public AnsiStringBuilder ResetPropertiesNewLine()
    {
        sb.Append('\n');
        sb.Append(AnsiRegistry.ResetProperties);
        return this;
    }

    public override string ToString() => sb.ToString();

    public void Clear()
    {
        sb.Clear();
        currentState = default;
    }
}
