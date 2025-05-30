
using Sunfire.Enums;

namespace Sunfire.Views;

public class Label : View
{
    public readonly List<TextFields> TextFields = [];

    public bool Highlighted = false;
    public bool Bold = false;
    public ConsoleColor TextColor = ConsoleColor.White;

    public override Task Draw()
    {
        //Console.WriteLine($"Origin: ({OriginX},{OriginY}), Size: <{SizeX},{SizeY}>, Text: {TextFields.First().Text}");

        if (!Highlighted)
        {
            Console.BackgroundColor = BackgroundColor;
            Console.ForegroundColor = TextColor;
        }
        else
        {
            Console.BackgroundColor = TextColor;
            Console.ForegroundColor = BackgroundColor;
        }

        var totalSize = SizeX * SizeY;

        var fullText = new string(' ', totalSize);
        var availableSize = totalSize;

        foreach (var textField in TextFields.OrderByDescending(o => o.Z))
        {
            int textLen = textField.Text.Length;
            if (availableSize <= 0)
                break;
                
            switch (textField.AlignSide)
            {
                case AlignSide.Left:

                    string additionLeft = textLen > (availableSize - 2) ?
                        additionLeft = textField.Text[..(availableSize - 2)] + "~ " :
                        additionLeft = textField.Text;

                    availableSize -= additionLeft.Length;

                    fullText = additionLeft + fullText[additionLeft.Length..];

                    break;
                case AlignSide.Right:

                    string additionRight = textField.Text.Length > (availableSize - 2) ?
                        additionRight = " ~" + textField.Text[^(availableSize - 2)..] :
                        additionRight = textField.Text;

                    availableSize -= additionRight.Length;

                    fullText = fullText[..(totalSize - additionRight.Length)] + additionRight;

                    break;
            }
        }

        string[] output = [.. fullText.Chunk(SizeX).Select(x => new string(x))];
        for (int i = 0; i < SizeY; i++)
        {
            Console.SetCursorPosition(OriginX, OriginY + i);

            var baseOutput = output[i];

            string finalOutput = Bold ? $"\x1b[1m{baseOutput}\x1b[0m" : baseOutput;

            Console.Write($"{finalOutput}");
        }

        return Task.CompletedTask;
    }

}

public class TextFields
{
    public int Z = 0;

    required public string Text;

    public AlignSide AlignSide = AlignSide.Left;
}
