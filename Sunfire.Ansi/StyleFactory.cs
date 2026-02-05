using Sunfire.Ansi.Models;

namespace Sunfire.Ansi;

public static class StyleFactory
{
    private static readonly StyleCache styleCache = new();

    public static int GetStyleId((SColor? fgColor, SColor? bgColor, SAnsiProperty properties) creationData) =>
        styleCache.GetOrAdd(creationData);

    public static StyleData Get(int id) =>
        styleCache.Get(id);
}
