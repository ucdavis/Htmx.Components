namespace Htmx.Components.Table.Models;

/// <summary>
/// Represents the current state of a table including sorting, filtering, and pagination information.
/// </summary>
public class TableState
{
    /// <summary>
    /// Gets or sets the name of the column currently being sorted.
    /// </summary>
    public string? SortColumn { get; set; }
    
    /// <summary>
    /// Gets or sets the direction of the current sort (e.g., "asc" or "desc").
    /// </summary>
    public string? SortDirection { get; set; } = "asc";
    
    /// <summary>
    /// Gets or sets the current page number (1-based).
    /// </summary>
    public int Page { get; set; } = 1;
    
    /// <summary>
    /// Gets or sets the number of rows to display per page.
    /// </summary>
    public int PageSize { get; set; } = 10;
    
    /// <summary>
    /// Gets or sets the collection of column filters applied to the table.
    /// </summary>
    public Dictionary<string, string> Filters { get; set; } = new();
    
    /// <summary>
    /// Gets or sets the collection of range filters for columns that support min/max filtering.
    /// </summary>
    public Dictionary<string, (string Min, string Max)> RangeFilters { get; set; } = new();
}
