using Moonfire.Input;
using Moonfire.Input.Enums;

namespace Sunfire;

public static class LegacyInputExtensions
{
   public static async Task EnableInputMode(
        this InputHandler inputHandler,
        Func<char, Task> textHandler,
        Func<Task> deletionHandler,
        List<(ConsoleKey key, Func<Task> task)> exitHandlers,
        List<(ConsoleKey key, Func<Task> task)>? specialHandlers = null,
        CancellationToken token = default)
    {
        // 1. Redirect input stream to a raw channel
        var rawChannel = inputHandler.OpenRaw();

        try
        {
            var textKeyHandlers = new Dictionary<ConsoleKey, Func<Task>>();
            bool shouldExit = false;

            // Register special key handlers
            if (specialHandlers is not null)
            {
                foreach (var (key, task) in specialHandlers)
                {
                    textKeyHandlers[key] = task;
                }
            }

            // Register exit key handlers with completion trigger
            foreach (var (key, task) in exitHandlers)
            {
                textKeyHandlers[key] = async () =>
                {
                    await task();
                    shouldExit = true;
                };
            }

            // Register backspace handler
            textKeyHandlers[ConsoleKey.Backspace] = deletionHandler;

            // 2. Read events directly from the raw channel
            await foreach (var evt in rawChannel.Reader.ReadAllAsync(token))
            {
                if (evt.Key.InputType != InputType.Keyboard)
                    continue;

                // Check for key action matches (Backspace, Enter, Escape, Arrow keys, etc.)
                if (evt.Key.KeyboardKey.HasValue && textKeyHandlers.TryGetValue(evt.Key.KeyboardKey.Value, out var handler))
                {
                    await handler();
                    
                    if (shouldExit)
                        break;
                }
                // Fall back to UTF-8 text character handling
                else if (evt.InputData.UTFChar.HasValue && evt.InputData.UTFChar.Value != '\0')
                {
                    await textHandler(evt.InputData.UTFChar.Value);
                }
            }
        }
        finally
        {
            // 3. Close the raw channel and restore normal keybindings
            inputHandler.CloseRaw();
        }
    }
}