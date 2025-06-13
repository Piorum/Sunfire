using SunfireFramework.Enums;
using SunfireFramework.Terminal;

namespace SunfireFramework.Views.TextBoxes;

public class LabelSVSlim : ISunfireView
{
    public int OriginX { set; get; }
    public int OriginY { set; get; }
    public int SizeX { set; get; }
    public int SizeY { set; get; }

    public TextProperty Properties = TextProperty.None;
    public Direction Alignment = Direction.Left;

    public ConsoleColor TextColor = ConsoleColor.White;
    public ConsoleColor BackgroundColor { get; set; } = ConsoleColor.Black;

    private ConsoleColor OutputTextColor = ConsoleColor.White;
    private ConsoleColor OutputBackgroundColor = ConsoleColor.Black;

    public string Text = "";
    private string compiledText = "";

    public Task Arrange()
    {
        var maxSize = SizeX * SizeY;

        compiledText = Alignment switch
        {
            Direction.Left => Text[..Math.Min(Text.Length, maxSize)] + new string(' ', Math.Max(0, maxSize - Text.Length)),
            Direction.Right => new string(' ', Math.Max(0, maxSize - Text.Length)) + Text[^Math.Min(Text.Length, maxSize)..],
            _ => throw new InvalidOperationException("Label has invalid alignment direction.")
        };

        if (Properties.HasFlag(TextProperty.Highlighted))
        {
            OutputTextColor = BackgroundColor;
            OutputBackgroundColor = TextColor;
        }
        else
        {
            OutputTextColor = TextColor;
            OutputBackgroundColor = BackgroundColor;
        }

        return Task.CompletedTask;
    }

    public async Task Draw()
    {
        var output = new TerminalOutput()
        {
            X = OriginX,
            Y = OriginY,
            Output = compiledText
        };

        await TerminalWriter.WriteAsync(output, backgroundColor: OutputBackgroundColor, foregroundColor: OutputTextColor);
    }
}
