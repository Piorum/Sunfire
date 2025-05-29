
using Sunfire.Enums;

namespace Sunfire.Views;

public class Label : View
{
    required public int Z;

    required public string LabelText;

    public AlignStyle AlignStyle = AlignStyle.Left;
    public WrapStyle WrapStyle = WrapStyle.Trim;
    public bool Highlighted = false;
}
