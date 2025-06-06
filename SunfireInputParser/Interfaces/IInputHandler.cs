namespace SunfireInputParser.Interfaces;

public interface IInputHandler
{
#if MOUSE_SUPPORT
    IMouseHandler MouseHandler { get; }
#endif
    IKeyboardHandler KeyboardHandler { get; }
}
