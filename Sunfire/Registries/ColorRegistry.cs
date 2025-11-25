using Sunfire.Ansi.Models;

namespace Sunfire.Registries;

public static class ColorRegistry
{
    public static readonly SColor Blue = new(59, 141, 234);
    public static readonly SColor Red = new(241, 76, 76);
    
    public static readonly SColor DirectoryColor = Blue;
    public static readonly SColor? FileColor = null;
    
}
