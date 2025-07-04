namespace Htmx.Components.Table.Models;

public class TableState
{
    public string? SortColumn { get; set; }
    public string? SortDirection { get; set; } = "asc";
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    
    public Dictionary<string, string> Filters { get; set; } = new();
    public Dictionary<string, (string Min, string Max)> RangeFilters { get; set; } = new();
}
