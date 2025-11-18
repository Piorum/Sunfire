using System.Threading.Channels;
using Sunfire.Input.Platforms.Linux;
using Sunfire.Input.Platforms.Fallback;
using Sunfire.Input.Models;

namespace Sunfire.Input;

public interface IInputTranslator
{
    //polling task that returns TerminalInput to InputHandler to send to Keyboard or Mouse input Handler
    Task PollInput(ChannelWriter<TerminalInput> writer, CancellationToken token);
}

internal static class InputTranslatorFactory
{
    public static IInputTranslator Create()
    {
        if(OperatingSystem.IsLinux())
            return new LinuxInputTranslator();
        else

            return new FallbackInputTranslator();
    }
}