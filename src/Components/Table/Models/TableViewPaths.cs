namespace Htmx.Components.Table.Models;

/// <summary>
/// Contains the default view paths for table component partial views.
/// </summary>
public class TableViewPaths
{
    /// <summary>
    /// Gets or sets the view path for the main table component.
    /// </summary>
    public string Table { get; set; } = "_Table";
    
    /// <summary>
    /// Gets or sets the view path for the table body component.
    /// </summary>
    public string Body { get; set; } = "_TableBody";
    
    /// <summary>
    /// Gets or sets the view path for individual table cells.
    /// </summary>
    public string Cell { get; set; } = "_TableCell";
    
    /// <summary>
    /// Gets or sets the view path for cell action lists.
    /// </summary>
    public string CellActionList { get; set; } = "_TableCellActionList";
    
    /// <summary>
    /// Gets or sets the view path for table-level action lists.
    /// </summary>
    public string TableActionList { get; set; } = "_TableActionList";
    
    /// <summary>
    /// Gets or sets the view path for date range filter controls.
    /// </summary>
    public string FilterDateRange { get; set; } = "_TableFilterDateRange";
    
    /// <summary>
    /// Gets or sets the view path for text filter controls.
    /// </summary>
    public string FilterText { get; set; } = "_TableFilterText";
    
    /// <summary>
    /// Gets or sets the view path for table headers.
    /// </summary>
    public string Header { get; set; } = "_TableHeader";
    
    /// <summary>
    /// Gets or sets the view path for pagination controls.
    /// </summary>
    public string Pagination { get; set; } = "_TablePagination";
    
    /// <summary>
    /// Gets or sets the view path for editable text cells.
    /// </summary>
    public string CellEditText { get; set; } = "_TableCellEditText";
    
    /// <summary>
    /// Gets or sets the view path for table rows.
    /// </summary>
    public string Row { get; set; } = "_TableRow";
    
    /// <summary>
    /// Gets or sets the view path for edit mode class toggle controls.
    /// </summary>
    public string EditClassToggle { get; set; } = "_TableEditClassToggle";
}