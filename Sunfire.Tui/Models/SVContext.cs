using System.Diagnostics.CodeAnalysis;

namespace Sunfire.Tui.Models;

/// <summary>
/// Minimal context struct that shifts the origin and bounds checks SVBuffer.
/// </summary>
public readonly struct SVContext
{
    public readonly int X;
    public readonly int Y;
    public readonly uint W;
    public readonly uint H;

    private readonly SVBuffer Buffer;

    [DoesNotReturn]
    private static void CreationOutOfBounds(int x,int y, int w, int h, SVBuffer buffer) =>
        throw new($"SVContext region is out of bounds for the SVBuffer [Parameters] X:\"{x}\" | Y:\"{y}\" | W:\"{w}\" | H:\"{h}\" : [Buffer Dimensions] W:\"{buffer.Width}\" | H:\"{buffer.Height}\"");
    [DoesNotReturn]
    private static void CreationOutOfBounds(int x,int y, int w, int h, SVContext context) => 
        throw new($"SVContext region is out of bounds for the existing SVContext [Parameters] X:\"{x}\" | Y:\"{y}\" | W:\"{w}\" | H:\"{h}\" : [Context Dimensions] X:\"{context.X}\" | Y: \"{context.Y}\" | W:\"{context.W}\" | H:\"{context.H}\"");
    [DoesNotReturn]
    private static void AccessOutOfBounds(int X, int Y, int W, int H, int x, int y) => 
        throw new($"Coordinate out of bounds for the current SVContext X:\"{X}\" | Y:\"{Y}\" | W:\"{W}\" | H:\"{H}\" : Coordinate:({x},{y})");
    [DoesNotReturn]
    private static void CopyTooLarge(uint sW, uint sH, uint dW, uint dH) => 
        throw new($"TerminalContext to large to copy to given TerminalContext Source W:\"{sW}\" | H:\"{sH}\" : Destination W:\"{dW}\" | H:\"{dH}\"");

    /// <param name="x">X coordinate of new origin.</param>
    /// <param name="y">Y coordinate of new origin.</param>
    /// <param name="w">Width of new context.</param>
    /// <param name="h">Height of new context.</param>
    /// <param name="buffer">Buffer that data will be referenced from.</param>
    public SVContext(int x, int y, int w, int h, SVBuffer buffer)
    {
        if(x < 0 || y < 0 || w < 0 || h < 0 || x + w > buffer.Width || y + h > buffer.Height)
            CreationOutOfBounds(x,y,w,h,buffer);

        Buffer = buffer;
        X = x;
        Y = y;
        W = (uint)w;
        H = (uint)h;
    }

    /// <param name="x">X coordinate of new origin.</param>
    /// <param name="y">Y coordinate of new origin.</param>
    /// <param name="w">Width of new context.</param>
    /// <param name="h">Height of new context.</param>
    /// <param name="context">Context that data will be referenced from.</param>
    public SVContext(int x, int y, int w, int h, SVContext context)
    {
        if(x < context.X || y < context.Y || w < 0 || h < 0 || x + w > context.X + context.W || y + h > context.Y + context.H)
            CreationOutOfBounds(x,y,w,h,context);

        Buffer = context.Buffer;
        X = x;
        Y = y;
        W = (uint)w;
        H = (uint)h;
    }

    /// <summary></summary>
    /// <returns>Cell at (x,y) relative to the top left origin (OriginX,OriginY)</returns>
    public ref SVCell this[int x, int y]
    {
        get
        {
            if((uint)x >= W || (uint)y >= H)
                AccessOutOfBounds(X,Y,(int)W,(int)H,x,y);

            return ref Buffer[X + x, Y + y];
        }
    }

    private ref SVCell Raw(int x, int y) =>
        ref Buffer [X + x, Y + y];

    public void CopyTo(SVContext destination)
    {
        if(destination.H < H || destination.W < W)
            CopyTooLarge(W, H, destination.W, destination.H);

        for(int y = 0; y < H; y++)
            for(int x = 0; x < W; x++)
                destination.Raw(x, y) = Raw(x, y);
    }

    public SVContextEnumerator GetEnumerator() => new(this);

    public ref struct SVContextEnumerator
    {
        private readonly SVContext _context;
        private int x;
        private int y;

        public SVContextEnumerator(SVContext context) =>
            (_context, x, y) = (context, 0, 0);

        public bool MoveNext()
        {
            x++;
            if(x >= _context.W)
            {
                x = 0;
                y++;
            }

            return y < _context.H;
        }

        public readonly ref SVCell Current => ref _context.Raw(x, y);
    }
}
