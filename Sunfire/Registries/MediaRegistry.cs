using System.Collections.ObjectModel;

namespace Sunfire.Registries;

public static class MediaRegistry
{
    //Icons from extension
    public static readonly ReadOnlyDictionary<string, char> Icons = new(new Dictionary<string, char>()
    {
        { ".yml", '' },
        { ".yaml", '' }
    });

    //Icons from full file name (Favored over extension based icon)
    public static readonly ReadOnlyDictionary<string, char> SpecialIcons = new(new Dictionary<string, char>()
    {
        { "dockerfile", '' },
        { "Dockerfile", '' },
        { "docker-compose.yml", '' },
        { "Docker-compose.yml", '' },
        { "docker-compose.yaml", '' },
        { "Docker-compose.yaml", '' }
    });


}
