namespace SunfireFramework;

public interface IRelativeSunfireView : ISunfireView
{
    int X { set; get; }
    int Y { set; get; }
    int Z { set; get; }
}

public interface ISunfireView
{
    int OriginX { set; get; } // Top Left
    int OriginY { set; get; } // Top Left
    int SizeX { set; get; } // Width
    int SizeY { set; get; } // Height

    public ConsoleColor BackgroundColor { set; get; }

    Task Arrange();

    Task Draw();
}
