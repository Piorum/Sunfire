namespace SunfireFramework.Enums;

[Flags]
public enum SVTextProperty : byte
{
    None = 0,
    Bold = 1,
    Italic = 2,
    Underline = 4,
    Highlight = 8,
    Strikethrough = 16
}
