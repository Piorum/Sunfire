namespace Sunfire.Tui.Interfaces;

/// <summary>
/// Cache for storing references to rich data (non characters) that a cell should display.
/// </summary>
public interface IRichDataCache
{
    /// <summary>
    /// Name of the data cache.
    /// </summary>
    string StoreType { get; }

    /// <summary>
    /// Gets rich data referenced by cell at (x, y) to render it at (x, y).
    /// </summary>
    /// <param name="dataId">Id of rich data in cache.</param>
    /// <returns>Raw ansi string to be output.</returns>
    string GetRenderString(int dataId);
}
