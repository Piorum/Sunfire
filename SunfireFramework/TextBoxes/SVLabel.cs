namespace SunfireFramework.TextBoxes;

public class SVLabel : ISunfireView
{
    public int OriginX { set; get; }
    public int OriginY { set; get; }
    public int SizeX { set; get; }
    public int SizeY { set; get; }

    public bool Bold = false;
    public bool Selected = false;
    public bool Underlined = false;
    public bool Highlighted = false;

    private string CompiledText = "";

    public ConsoleColor TextColor = ConsoleColor.White;
    public ConsoleColor BackgroundColor { get; set; } = ConsoleColor.Black;

    required public List<TextField> TextFields = [];

    public Task Arrange()
    {
        //Compile text from TextFields
        return Task.CompletedTask;
    }

    public Task Draw()
    {
        //Output Compiled Text

        //Test code
        if (Highlighted)
        {
            Console.ForegroundColor = BackgroundColor;
            Console.BackgroundColor = TextColor;
        }
        else
        {
            Console.ForegroundColor = TextColor;
            Console.BackgroundColor = BackgroundColor;
        }

        var textField = TextFields.First();

        Console.SetCursorPosition(OriginX, OriginY);
        Console.Write(textField.Text);

        return Task.CompletedTask;
    }
}
