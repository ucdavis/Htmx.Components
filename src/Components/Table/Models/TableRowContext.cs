using System.Text.Json;
using Htmx.Components.Extensions;
using Htmx.Components.Models;

namespace Htmx.Components.Table.Models;

public interface ITableRowContext : IOobTargetable
{
    object Item { get; }
    string RowId { get; } // E.g., "row_5f3e"
    int PageIndex { get; } // Row's index within current page
    string Key { get; }
    bool IsEditing { get; }
}

public class TableRowContext<T, TKey> : ITableRowContext
    where T : class
{
    public required T Item { get; set; }
    public required ModelHandler<T, TKey> ModelHandler { get; set; }
    public string RowId => "row_" + StringKey.SanitizeForHtmlId();
    public int PageIndex { get; set; } = 0; // Row's index within current page
    private TKey? _key = default!;
    public TKey? Key
    {
        get => _key;
        set
        {
            _key = value;
            StringKey = EqualityComparer<TKey>.Default.Equals(value, default) ? "" : JsonSerializer.Serialize(value);
        }
    }
    public string StringKey { get; set; } = "";
    string ITableRowContext.Key => StringKey;
    object ITableRowContext.Item => Item!;
    public string? TargetSelector { get; set; } = null;
    public bool IsEditing { get; set; } = false;
    public OobTargetDisposition? TargetDisposition { get; set; } = OobTargetDisposition.OuterHtml;
}
