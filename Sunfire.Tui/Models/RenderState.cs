using Sunfire.Ansi.Models;

namespace Sunfire.Tui.Models;

public class RenderState(int bufferSize)
{
    //Oversized to allow for DWC, ZWC, etc...
    public char[] OutputBuffer = new char[bufferSize];
    public int OutputIndex;
    public int CursorMovement;

    public SStyle CurrentStyle;

    public (int X, int Y) OutputStart;
    public (int X, int Y) Cursor;

    public void Reset()
    {
        (OutputIndex, CursorMovement) = (0, 0);

        CurrentStyle = new(null, null, SAnsiProperty.None, (0, 0));

        OutputStart = (0, 0);
        Cursor = (-1, -1);
    }
}
