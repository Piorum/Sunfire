using System.Runtime.CompilerServices;
using SunfireInputParser.Interfaces;

namespace SunfireInputParser.Linux;

public class LinuxInputHandler : IInputHandler
{
#if MOUSE_SUPPORT
    public IMouseHandler MouseHandler { private set; get; } = new LinuxMouseHandler();
#endif
    public IKeyboardHandler KeyboardHandler { private set; get; } = new LinuxKeyboardHandler();
}
