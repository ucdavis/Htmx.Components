using System.Linq.Expressions;
using System.Text.Json;
using FastExpressionCompiler;
using Htmx.Components.Extensions;
using Htmx.Components.Models;

namespace Htmx.Components.Table.Models;

// We have a non-generic interface, since razor views don't support generic type params
public interface ITableColumnModel
{
    string Header { get; set; }
    string DataName { get; set; }
    string Id { get; }
    bool Sortable { get; set; }
    bool Filterable { get; set; }
    bool IsEditable { get; set; }
    ColumnType ColumnType { get; set; }
    string? CellPartialView { get; set; } // Custom rendering for cell
    string? FilterPartialView { get; set; } // Custom rendering for filter
    public string? CellEditPartialView { get; set; }
    Task<IEnumerable<ActionModel>> GetActionsAsync(ITableRowContext rowContext);
    object GetValue(ITableRowContext rowContext);  // Extracts value dynamically
    string GetSerializedValue(ITableRowContext rowContext);
    ITableModel Table { get; set; } // Reference to the parent table
    Func<ITableRowContext, Task<IInputModel>> GetInputModel { get; }
}

public enum ColumnType
{
    ValueSelector,
    Display
}

/// <summary>
/// Internal model class used by the framework to represent table cell data in partial views.
/// This class should not be instantiated directly in user code.
/// </summary>
/// <remarks>
/// This class is used internally by table rendering logic to pass context data
/// between table views and cell partial views.
/// </remarks>
internal class TableCellPartialModel
{
    public required ITableModel Table { get; init; }
    public required ITableRowContext Row { get; init; }
    public required ITableColumnModel Column { get; init; }
}

public class TableColumnModel<T, TKey> : ITableColumnModel where T : class
{
    internal TableColumnModel(TableColumnModelConfig<T, TKey> config)
    {
        Header = config.Display.Header;
        DataName = config.Display.DataName;
        Sortable = config.Behavior.Sortable;
        Filterable = config.Behavior.Filterable;
        IsEditable = config.Behavior.IsEditable;
        ColumnType = config.Display.ColumnType;
        CellPartialView = config.Display.CellPartialView;
        FilterPartialView = config.Display.FilterPartialView;
        CellEditPartialView = config.Display.CellEditPartialView;
        Filter = config.FilterOptions.Filter;
        RangeFilter = config.FilterOptions.RangeFilter;
        ActionsFactories = config.ActionOptions.ActionsFactories;
        GetInputModel = config.InputOptions.GetInputModel;
        SelectorExpression = config.DataOptions.SelectorExpression ?? (x => x!);
        SelectorFunc = config.DataOptions.SelectorFunc ?? SelectorExpression.CompileFast();
        ModelHandler = config.DataOptions.ModelHandler!;
    }

    public ModelHandler<T, TKey> ModelHandler { get; set; } = default!;

    private Expression<Func<T, object>> _selectorExpression = x => x!;

    // used for referencing a member of T when working with IQueryable<T>
    public Expression<Func<T, object>> SelectorExpression
    {
        get => _selectorExpression;
        set
        {
            _selectorExpression = value;
            // FastExpressionCompiler is a widely used and maintained library that will
            // fallback to the built-in Expression.Compile() if it encounters an error.
            SelectorFunc = value.CompileFast();
        }
    }

    // used for actually pulling values from instance of T when rendering cells
    public Func<T, object> SelectorFunc { get; set; } = x => x!;
    public string Header { get; set; } = "";
    public string DataName { get; set; } = "";
    public string Id => "col_" + DataName.SanitizeForHtmlId();
    public bool Sortable { get; set; } = true;
    public bool Filterable { get; set; } = true;
    public bool IsEditable { get; set; } = false;
    public ColumnType ColumnType { get; set; }
    public string? CellPartialView { get; set; } // Custom rendering for cell
    public string? FilterPartialView { get; set; } // Custom rendering for filter
    public string? CellEditPartialView { get; set; }
    public ITableModel Table { get; set; } = default!;

    /// <summary>
    /// A delegate that extends filtering of a <see cref="IQueryable<typeparamref name="T"/>"/>   using a single value comparison
    /// </summary>
    public Func<IQueryable<T>, string, IQueryable<T>>? Filter { get; set; }

    /// <summary>
    /// A delegate that extends filtering of a <see cref="IQueryable<typeparamref name="T"/>"/>  using a two value range comparison
    /// </summary>
    public Func<IQueryable<T>, string, string, IQueryable<T>>? RangeFilter { get; set; }

    /// <summary>
    /// A delegate that generates one or more <see cref="ActionModel"/>s that can be mapped to buttons in a view
    /// </summary>
    public List<Func<TableRowContext<T, TKey>, Task<IEnumerable<ActionModel>>>> ActionsFactories { get; set; } = [];

    internal Func<TableRowContext<T, TKey>, Task<IInputModel>>? GetInputModel { get; set; }

    Func<ITableRowContext, Task<IInputModel>> ITableColumnModel.GetInputModel => async rowContext =>
    {
        if (rowContext is not TableRowContext<T, TKey> typedRowContext)
            throw new InvalidOperationException("Row context is not of the expected type.");
        if (GetInputModel == null)
            throw new InvalidOperationException("GetInputModel is not set.");

        return await GetInputModel(typedRowContext);
    };

    public async Task<IEnumerable<ActionModel>> GetActionsAsync(ITableRowContext rowContext)
    {
        if (rowContext.Item is T typedItem)
        {
            var results = new List<ActionModel>();
            foreach (var factory in ActionsFactories)
            {
                var actions = await factory((TableRowContext<T, TKey>)rowContext);
                if (actions != null)
                {
                    results.AddRange(actions);
                }
            }
            return results;
        }
        return [];
    }

    // Implement the interface method for non-generic use
    public object GetValue(ITableRowContext rowContext)
    {
        if (rowContext.Item is T typedItem)
        {
            return SelectorFunc(typedItem);
        }
        return "";
    }

    public string GetSerializedValue(ITableRowContext rowContext)
    {
        if (rowContext.Item is T typedItem)
        {
            return JsonSerializer.Serialize(SelectorFunc(typedItem));
        }
        return "";
    }
}

internal class TableColumnModelConfig<T, TKey>
    where T : class
{
    public TableColumnDisplayOptions Display { get; set; } = new();
    public TableColumnBehaviorOptions Behavior { get; set; } = new();
    public TableColumnFilterOptions<T> FilterOptions { get; set; } = new();
    public TableColumnActionOptions<T, TKey> ActionOptions { get; set; } = new();
    public TableColumnInputOptions<T, TKey> InputOptions { get; set; } = new();
    public TableColumnDataOptions<T, TKey> DataOptions { get; set; } = default!;
}

internal class TableColumnDisplayOptions
{
    public string Header { get; set; } = "";
    public string DataName { get; set; } = "";
    public string? CellPartialView { get; set; }
    public string? FilterPartialView { get; set; }
    public string? CellEditPartialView { get; set; }
    public ColumnType ColumnType { get; set; } = ColumnType.ValueSelector;
}

internal class TableColumnBehaviorOptions
{
    public bool Sortable { get; set; } = true;
    public bool Filterable { get; set; } = false;
    public bool IsEditable { get; set; } = false;
}

internal class TableColumnFilterOptions<T>
    where T : class
{
    public Func<IQueryable<T>, string, IQueryable<T>>? Filter { get; set; }
    public Func<IQueryable<T>, string, string, IQueryable<T>>? RangeFilter { get; set; }
}

internal class TableColumnActionOptions<T, TKey>
    where T : class
{
    public List<Func<TableRowContext<T, TKey>, Task<IEnumerable<ActionModel>>>> ActionsFactories { get; set; } = [];
}

internal class TableColumnInputOptions<T, TKey>
    where T : class
{
    public Func<TableRowContext<T, TKey>, Task<IInputModel>>? GetInputModel { get; set; }
}

internal class TableColumnDataOptions<T, TKey>
    where T : class
{
    public Expression<Func<T, object>>? SelectorExpression { get; set; }
    public Func<T, object>? SelectorFunc { get; set; }
    public ModelHandler<T, TKey>? ModelHandler { get; set; }
}