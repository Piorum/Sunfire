#if MOUSE_SUPPORT
using SunfireInputParser.Mouse;
using SunfireInputParser.Mouse.Platforms;
#endif
using SunfireInputParser.Keyboard;
using SunfireInputParser.Keyboard.Platforms;

namespace SunfireInputParser;

public class LinuxInputHandler
{
#if MOUSE_SUPPORT
    public IMouseHandler MouseHandler { private set; get; } = new LinuxMouseHandler();
#endif
    public IKeyboardHandler KeyboardHandler { private set; get; } = new LinuxKeyboardHandler();
}
