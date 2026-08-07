using Moonfire.Ansi.Models;

namespace Sunfire.Registries;

public static class ColorRegistry
{
    public static readonly AnsiTruecolor Blue = new(59, 141, 234);
    public static readonly AnsiTruecolor Red = new(241, 76, 76);
    public static readonly AnsiTruecolor Yellow = new(239,252,122);
     
    public static readonly AnsiTruecolor DirectoryColor = Blue;
    public static readonly AnsiTruecolor? FileColor = null;
    
}
