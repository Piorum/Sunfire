namespace Sunfire.Tui.Models;

/// <summary>
/// Reference to Rich Data
/// </summary>
/// <param name="StoreId">Reference to Id of IRichDataCache</param>
/// <param name="DataId">Reference to Id of rich data within IRichDataCache</param>
public readonly record struct RichDataRef(
    int StoreId,
    int DataId
);
