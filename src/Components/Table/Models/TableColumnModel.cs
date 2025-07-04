using System.Linq.Expressions;
using System.Text.Json;
using FastExpressionCompiler;
using Htmx.Components.Extensions;
using Htmx.Components.Models;

namespace Htmx.Components.Table.Models;

// We have a non-generic interface, since razor views don't support generic type params
/// <summary>
/// Represents a column in a table model with configuration for display, filtering, and editing capabilities.
/// </summary>
public interface ITableColumnModel
{
    /// <summary>
    /// Gets or sets the display header text for the column.
    /// </summary>
    string Header { get; set; }
    
    /// <summary>
    /// Gets or sets the data property name that this column represents.
    /// </summary>
    string DataName { get; set; }
    
    /// <summary>
    /// Gets the unique identifier for this column.
    /// </summary>
    string Id { get; }
    
    /// <summary>
    /// Gets or sets whether this column can be sorted.
    /// </summary>
    bool Sortable { get; set; }
    
    /// <summary>
    /// Gets or sets whether this column can be filtered.
    /// </summary>
    bool Filterable { get; set; }
    
    /// <summary>
    /// Gets or sets whether this column can be edited inline.
    /// </summary>
    bool IsEditable { get; set; }
    
    /// <summary>
    /// Gets or sets the type of column (value selector or display).
    /// </summary>
    ColumnType ColumnType { get; set; }
    
    /// <summary>
    /// Gets or sets the custom partial view for rendering the cell content.
    /// </summary>
    string? CellPartialView { get; set; }
    
    /// <summary>
    /// Gets or sets the custom partial view for rendering the filter control.
    /// </summary>
    string? FilterPartialView { get; set; }
    
    /// <summary>
    /// Gets or sets the custom partial view for rendering the cell in edit mode.
    /// </summary>
    public string? CellEditPartialView { get; set; }
    
    /// <summary>
    /// Gets the available actions for a specific row in this column.
    /// </summary>
    /// <param name="rowContext">The context of the row to get actions for.</param>
    /// <returns>A collection of action models for the row.</returns>
    Task<IEnumerable<ActionModel>> GetActionsAsync(ITableRowContext rowContext);
    
    /// <summary>
    /// Extracts the value for this column from the given row context.
    /// </summary>
    /// <param name="rowContext">The row context to extract the value from.</param>
    /// <returns>The column value for the specified row.</returns>
    object GetValue(ITableRowContext rowContext);
    
    /// <summary>
    /// Gets the serialized string representation of the column value for the given row.
    /// </summary>
    /// <param name="rowContext">The row context to get the serialized value from.</param>
    /// <returns>The serialized column value.</returns>
    string GetSerializedValue(ITableRowContext rowContext);
    
    /// <summary>
    /// Gets or sets the reference to the parent table model.
    /// </summary>
    ITableModel Table { get; set; }
    
    /// <summary>
    /// Gets the function that creates an input model for editing this column.
    /// </summary>
    Func<ITableRowContext, Task<IInputModel>> GetInputModel { get; }
}

/// <summary>
/// Specifies the type of table column.
/// </summary>
public enum ColumnType
{
    /// <summary>
    /// A column that displays data using a property selector expression.
    /// </summary>
    ValueSelector,
    
    /// <summary>
    /// A column that displays data using custom display logic.
    /// </summary>
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

/// <summary>
/// Represents a table column model that provides configuration and behavior for displaying and editing entity data in a table.
/// </summary>
/// <typeparam name="T">The entity type that this column operates on.</typeparam>
/// <typeparam name="TKey">The key type for the entity.</typeparam>
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

    /// <summary>
    /// Gets or sets the model handler responsible for data operations on the entity type.
    /// </summary>
    public ModelHandler<T, TKey> ModelHandler { get; set; } = default!;

    private Expression<Func<T, object>> _selectorExpression = x => x!;

    /// <summary>
    /// Gets or sets the expression used to select the property value for this column.
    /// Used for referencing a member of T when working with IQueryable&lt;T&gt;.
    /// </summary>
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

    /// <summary>
    /// Gets or sets the compiled function used to extract values from instances of T when rendering cells.
    /// </summary>
    public Func<T, object> SelectorFunc { get; set; } = x => x!;
    
    /// <summary>
    /// Gets or sets the display header text for the column.
    /// </summary>
    public string Header { get; set; } = "";
    
    /// <summary>
    /// Gets or sets the data property name that this column represents.
    /// </summary>
    public string DataName { get; set; } = "";
    
    /// <summary>
    /// Gets the unique identifier for this column.
    /// </summary>
    public string Id => "col_" + DataName.SanitizeForHtmlId();
    
    /// <summary>
    /// Gets or sets whether this column can be sorted.
    /// </summary>
    public bool Sortable { get; set; } = true;
    
    /// <summary>
    /// Gets or sets whether this column can be filtered.
    /// </summary>
    public bool Filterable { get; set; } = true;
    
    /// <summary>
    /// Gets or sets whether this column can be edited inline.
    /// </summary>
    public bool IsEditable { get; set; } = false;
    
    /// <summary>
    /// Gets or sets the type of column (value selector or display).
    /// </summary>
    public ColumnType ColumnType { get; set; }
    
    /// <summary>
    /// Gets or sets the custom partial view for rendering the cell content.
    /// </summary>
    public string? CellPartialView { get; set; }
    
    /// <summary>
    /// Gets or sets the custom partial view for rendering the filter control.
    /// </summary>
    public string? FilterPartialView { get; set; }
    
    /// <summary>
    /// Gets or sets the custom partial view for rendering the cell in edit mode.
    /// </summary>
    public string? CellEditPartialView { get; set; }
    
    /// <summary>
    /// Gets or sets the reference to the parent table model.
    /// </summary>
    public ITableModel Table { get; set; } = default!;

    /// <summary>
    /// Gets or sets a delegate that extends filtering of a <see cref="IQueryable{T}"/> using a single value comparison.
    /// </summary>
    public Func<IQueryable<T>, string, IQueryable<T>>? Filter { get; set; }

    /// <summary>
    /// Gets or sets a delegate that extends filtering of a <see cref="IQueryable{T}"/> using a two value range comparison.
    /// </summary>
    public Func<IQueryable<T>, string, string, IQueryable<T>>? RangeFilter { get; set; }

    /// <summary>
    /// Gets or sets a collection of factory functions that generate action models for table rows.
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

    /// <summary>
    /// Gets the available actions for a specific row in this column.
    /// </summary>
    /// <param name="rowContext">The context of the row to get actions for.</param>
    /// <returns>A collection of action models for the row.</returns>
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

    /// <summary>
    /// Extracts the value for this column from the given row context.
    /// </summary>
    /// <param name="rowContext">The row context to extract the value from.</param>
    /// <returns>The column value for the specified row.</returns>
    public object GetValue(ITableRowContext rowContext)
    {
        if (rowContext.Item is T typedItem)
        {
            return SelectorFunc(typedItem);
        }
        return "";
    }

    /// <summary>
    /// Gets the serialized string representation of the column value for the given row.
    /// </summary>
    /// <param name="rowContext">The row context to get the serialized value from.</param>
    /// <returns>The serialized column value.</returns>
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