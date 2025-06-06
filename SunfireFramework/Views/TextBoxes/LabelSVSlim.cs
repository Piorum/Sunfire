using SunfireFramework.Enums;
using SunfireFramework.Terminal;

namespace SunfireFramework.Views.TextBoxes;

public class LabelSVSlim : ISunfireView
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
        var textField = TextFields.FirstOrDefault();
        if(textField is null) return;

        var output = new TerminalOutput()
        {
            X = OriginX,
            Y = OriginY,
            Output = textField.Text
        };

        if (Properties.Contains(TextProperty.Highlighted))
            await TerminalWriter.WriteAsync(output, backgroundColor: TextColor, foregroundColor: BackgroundColor);
        else
            await TerminalWriter.WriteAsync(output, backgroundColor: BackgroundColor, foregroundColor: TextColor);
    }
}
