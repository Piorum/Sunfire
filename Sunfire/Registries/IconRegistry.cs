using System.Collections.ObjectModel;
using Sunfire.Ansi.Models;

namespace Sunfire.Registries;

public static class IconRegistry
{
    //Icons from extension
    public static readonly ReadOnlyDictionary<string, (string icon, SStyle style)> Icons = new(new Dictionary<string, (string, SStyle)>()
    {
        { ".yml", (" ", new(ColorRegistry.FileColor)) },
        { ".yaml", (" ", new(ColorRegistry.FileColor)) }
    });

    //Icons from full file name (Favored over extension based icon)
    public static readonly ReadOnlyDictionary<string, (string, SStyle)> SpecialIcons = new(new Dictionary<string, (string, SStyle)>()
    {
        { "dockerfile", (" ", new(new(0,0,255))) },
        { "Dockerfile", (" ", new(new(0,0,255))) },
        { "docker-compose.yml", (" ", new(new(0,0,255))) },
        { "Docker-compose.yml", (" ", new(new(0,0,255))) },
        { "docker-compose.yaml", (" ", new(new(0,0,255))) },
        { "Docker-compose.yaml", (" ", new(new(0,0,255))) }
    });

    public static readonly string DirectoryIcon = " ";

    public static readonly string FileIcon = " ";

}
