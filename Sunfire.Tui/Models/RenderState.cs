using Sunfire.Ansi.Models;

namespace Sunfire.Tui.Models;

public class RenderState(int bufferSize = 4096)
{
    //Oversized to allow for DWC, ZWC, etc...
    public byte[] OutputBuffer = new byte[bufferSize];
    public int OutputIndex;
    public int CursorMovement;

    public int CurrentStyleId;
    public StyleData CurrentStyle = new();

    public (int X, int Y) OutputStart;
    public (int X, int Y) Cursor;

    public void Reset()
    {
        OutputIndex = 0;
        CursorMovement = 0;

        CurrentStyle = new();

        OutputStart = (0, 0);
        Cursor = (-1, -1);
    }
}
