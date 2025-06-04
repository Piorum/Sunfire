using SunfireFramework.Enums;

namespace SunfireFramework.TextBoxes;

public class SVLabelSlim : ISunfireView
{
    public int OriginX { set; get; }
    public int OriginY { set; get; }
    public int SizeX { set; get; }
    public int SizeY { set; get; }

    required public HashSet<TextProperty> Properties = [];

    //private string CompiledText = "";

    public ConsoleColor TextColor = ConsoleColor.White;
    public ConsoleColor BackgroundColor { get; set; } = ConsoleColor.Black;

    required public List<TextField> TextFields = [];

    public Task Arrange()
    {
        //Compile text from TextFields
        return Task.CompletedTask;
    }

    public async Task Draw()
    {
        //Output Compiled Text

        //Test code
        var textField = TextFields.First();

        var output = new ConsoleOutput()
        {
            X = OriginX,
            Y = OriginY,
            Output = textField.Text
        };

        if (Properties.Contains(TextProperty.Highlighted))
            await ConsoleWriter.WriteAsync(output, backgroundColor: TextColor, foregroundColor: BackgroundColor);
        else
            await ConsoleWriter.WriteAsync(output, backgroundColor: BackgroundColor, foregroundColor: TextColor);
    }
}
