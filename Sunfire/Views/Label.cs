
using Sunfire.Enums;

namespace Sunfire.Views;

public class Label : View
{
    public readonly List<TextFields> TextFields = [];

    public bool Highlighted = false;
    public bool Bold = false;
    public ConsoleColor TextColor = ConsoleColor.White;
}

public class TextFields
{
    public int Z = 0;

    required public string Text;

    public AlignStyle AlignStyle = AlignStyle.Left;
    public WrapStyle WrapStyle = WrapStyle.Trim;
}
