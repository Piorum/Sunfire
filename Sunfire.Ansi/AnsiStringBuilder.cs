using System.Text;
using Sunfire.Ansi.Models;
using Sunfire.Ansi.Registries;

namespace Sunfire.Ansi;

public class AnsiStringBuilder(int bufferSize = 2<<15)
{
    private byte[] buffer = new byte[bufferSize];
    private int position = 0;
    private StyleData currentState = new();

    public bool IsEmpty => position == 0;
    
    public AnsiStringBuilder Append(string text, StyleData desiredState, (int X, int Y)? desiredCursorPos) =>
        Append(Encoding.UTF8.GetBytes(text) , desiredState, desiredCursorPos);

    public AnsiStringBuilder Append(ReadOnlySpan<byte> text, StyleData desiredState, (int X, int Y)? desiredCursorPos)
    {
        UpdateStyle(desiredState, desiredCursorPos);

        if (text.Length > 0)
        {
            EnsureCapacity(text.Length);
            text.CopyTo(buffer.AsSpan(position));
            position += text.Length;
        }

        return this;
    }
    private void AppendRaw(byte utf8Data)
    {
        EnsureCapacity(1);
        buffer[position++] = utf8Data;
    }
    private void AppendRaw(ReadOnlySpan<byte> utf8Data)
    {
        EnsureCapacity(utf8Data.Length);
        utf8Data.CopyTo(buffer.AsSpan(position));
        position += utf8Data.Length;
    }

    private void UpdateStyle(StyleData desiredState, (int X, int Y)? desiredCursorPos)
    {
        if (desiredCursorPos is { } pos)
        {
            EnsureCapacity(AnsiRegistry.MaxMoveCursorBytes);
            position += AnsiRegistry.MoveCursor(buffer.AsSpan(position), pos.Y, pos.X);
        }
        if (currentState.ForegroundColor != desiredState.ForegroundColor)
        {
            EnsureCapacity(AnsiRegistry.MaxSetColorBytes);
            position += AnsiRegistry.SetForegroundColor(buffer.AsSpan(position), desiredState.ForegroundColor);
        }
        if (currentState.BackgroundColor != desiredState.BackgroundColor)
        {
            EnsureCapacity(AnsiRegistry.MaxSetColorBytes);
            position += AnsiRegistry.SetBackgroundColor(buffer.AsSpan(position), desiredState.BackgroundColor);
        }

        var removedProperties = currentState.Properties & ~desiredState.Properties;
        var addedProperties = desiredState.Properties & ~currentState.Properties;

        if(addedProperties.HasFlag(SAnsiProperty.Bold))
            AppendRaw(AnsiRegistry.BoldBytes);
        else if (removedProperties.HasFlag(SAnsiProperty.Bold))
            AppendRaw(AnsiRegistry.DisableBoldBytes);

        if(addedProperties.HasFlag(SAnsiProperty.Italic))
            AppendRaw(AnsiRegistry.ItalicBytes);
        else if (removedProperties.HasFlag(SAnsiProperty.Italic))
            AppendRaw(AnsiRegistry.DisableItalicBytes);

        if(addedProperties.HasFlag(SAnsiProperty.Underline))
            AppendRaw(AnsiRegistry.UnderlineBytes);
        else if (removedProperties.HasFlag(SAnsiProperty.Underline))
            AppendRaw(AnsiRegistry.DisableUnderlineBytes);

        if(addedProperties.HasFlag(SAnsiProperty.Highlight))
            AppendRaw(AnsiRegistry.ReverseVideoModeBytes);
        else if (removedProperties.HasFlag(SAnsiProperty.Highlight))
            AppendRaw(AnsiRegistry.DisableReverseVideoModeBytes);

        if(addedProperties.HasFlag(SAnsiProperty.Strikethrough))
            AppendRaw(AnsiRegistry.StrikethroughBytes);
        else if (removedProperties.HasFlag(SAnsiProperty.Strikethrough))
            AppendRaw(AnsiRegistry.DisableStrikethroughBytes);

        currentState = desiredState;
    }

    public AnsiStringBuilder ShowCursor()
    {
        AppendRaw(AnsiRegistry.ShowCursorBytes);
        return this;
    }
    public AnsiStringBuilder HideCursor()
    {
        AppendRaw(AnsiRegistry.HideCursorBytes);
        return this;
    }

    public AnsiStringBuilder ResetProperties()
    {
        AppendRaw(AnsiRegistry.ResetPropertiesBytes);
        return this;
    }
    public AnsiStringBuilder ResetPropertiesNewLine()
    {
        AppendRaw((byte)'\n');
        AppendRaw(AnsiRegistry.ResetPropertiesBytes);
        return this;
    }

    private void EnsureCapacity(int needed)
    {
        if (position + needed > buffer.Length)
            Array.Resize(ref buffer, Math.Max(buffer.Length * 2, position + needed));
    }

    public ReadOnlyMemory<byte> ToBuffer() => buffer.AsMemory(0, position);
    public override string ToString() => Encoding.UTF8.GetString(buffer.AsSpan(0, position));

    public void Clear()
    {
        position = 0;
        currentState = new();
    }
}
