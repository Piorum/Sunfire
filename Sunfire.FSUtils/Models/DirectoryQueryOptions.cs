using Sunfire.FSUtils.Enums;

namespace Sunfire.FSUtils.Models;

public class DirectoryQueryOptions
{
    public string? SearchPattern { get; set; }
    public SortField SortBy { get; set; } = SortField.Name;
    public SortDirection SortDirection { get; set; } = SortDirection.Ascending;
    public SortOrder SortOrder { get; set; } = SortOrder.Mixed;
    public bool ShowHidden { get; set; } = true;
}
