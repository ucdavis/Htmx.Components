using System.Text.Json;
using Htmx.Components.Extensions;
using Htmx.Components.Models;

namespace Htmx.Components.Table.Models;

/// <summary>
/// Represents the context for a table row, providing access to row data and metadata.
/// </summary>
public interface ITableRowContext : IOobTargetable
{
    /// <summary>
    /// Gets the data item associated with this row.
    /// </summary>
    object Item { get; }
    
    /// <summary>
    /// Gets the unique identifier for this row (e.g., "row_5f3e").
    /// </summary>
    string RowId { get; }
    
    /// <summary>
    /// Gets the row's index within the current page.
    /// </summary>
    int PageIndex { get; }
    
    /// <summary>
    /// Gets the string representation of the row's key.
    /// </summary>
    string Key { get; }
    
    /// <summary>
    /// Gets a value indicating whether this row is currently in edit mode.
    /// </summary>
    bool IsEditing { get; }
}

/// <summary>
/// Provides context information for a specific table row with strongly-typed access to the row data.
/// </summary>
/// <typeparam name="T">The entity type displayed in the row.</typeparam>
/// <typeparam name="TKey">The key type for the entity.</typeparam>
public class TableRowContext<T, TKey> : ITableRowContext
    where T : class
{
    /// <summary>
    /// Gets or sets the data item associated with this row.
    /// </summary>
    public required T Item { get; set; }
    
    /// <summary>
    /// Gets or sets the model handler responsible for data operations.
    /// </summary>
    public required ModelHandler<T, TKey> ModelHandler { get; set; }
    /// <summary>
    /// Gets the unique identifier for this row (e.g., "row_5f3e").
    /// </summary>
    public string RowId => "row_" + StringKey.SanitizeForHtmlId();
    
    /// <summary>
    /// Gets or sets the row's index within the current page.
    /// </summary>
    public int PageIndex { get; set; } = 0;
    
    private TKey? _key = default!;
    
    /// <summary>
    /// Gets or sets the strongly-typed key for this row's entity.
    /// </summary>
    public TKey? Key
    {
        get => _key;
        set
        {
            _key = value;
            StringKey = EqualityComparer<TKey>.Default.Equals(value, default) ? "" : JsonSerializer.Serialize(value);
        }
    }
    /// <summary>
    /// Gets or sets the string representation of the row's key.
    /// </summary>
    public string StringKey { get; set; } = "";
    
    string ITableRowContext.Key => StringKey;
    object ITableRowContext.Item => Item!;
    
    /// <summary>
    /// Gets or sets the HTMX target selector for out-of-band updates.
    /// </summary>
    public string? TargetSelector { get; set; } = null;
    
    /// <summary>
    /// Gets or sets a value indicating whether this row is currently in edit mode.
    /// </summary>
    public bool IsEditing { get; set; } = false;
    
    /// <summary>
    /// Gets or sets the target disposition for out-of-band HTMX operations.
    /// </summary>
    public OobTargetDisposition? TargetDisposition { get; set; } = OobTargetDisposition.OuterHtml;
}
