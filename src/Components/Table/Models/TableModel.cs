using System.Linq.Expressions;
using Htmx.Components.Models;

namespace Htmx.Components.Table.Models;

/// <summary>
/// Represents a table model interface for use in non-generic contexts such as Razor views.
/// </summary>
public interface ITableModel
{
    /// <summary>
    /// Gets or sets the unique type identifier for the table model.
    /// </summary>
    public string TypeId { get; set; }
    
    /// <summary>
    /// Gets or sets the collection of row contexts representing the table data.
    /// </summary>
    public List<ITableRowContext> Rows { get; set; }
    
    /// <summary>
    /// Gets or sets the collection of column models defining the table structure.
    /// </summary>
    public List<ITableColumnModel> Columns { get; set; }
    
    /// <summary>
    /// Gets or sets the total number of pages available for pagination.
    /// </summary>
    public int PageCount { get; set; }
    
    /// <summary>
    /// Gets or sets the current state of the table including sorting, filtering, and pagination.
    /// </summary>
    public TableState State { get; set; }
    
    /// <summary>
    /// Gets or sets the model handler responsible for data operations.
    /// </summary>
    public ModelHandler ModelHandler { get; set; }
    
    /// <summary>
    /// Gets the available actions for the table.
    /// </summary>
    /// <returns>A collection of action models for the table.</returns>
    public Task<IEnumerable<ActionModel>> GetActionsAsync();
}

/// <summary>
/// Represents a strongly-typed table model for displaying and managing tabular data.
/// </summary>
/// <typeparam name="T">The entity type displayed in the table.</typeparam>
/// <typeparam name="TKey">The key type for the entity.</typeparam>
public class TableModel<T, TKey> : ITableModel
    where T : class
{
    /// <summary>
    /// Gets or sets the unique type identifier for the table model.
    /// </summary>
    public string TypeId { get; set; } = typeof(T).Name;
    
    /// <summary>
    /// Gets or sets the collection of row contexts representing the table data.
    /// </summary>
    public List<TableRowContext<T, TKey>> Rows { get; set; } = new();
    
    /// <summary>
    /// Gets or sets the collection of column models defining the table structure.
    /// </summary>
    public List<TableColumnModel<T, TKey>> Columns { get; set; } = new();
    
    /// <summary>
    /// Gets or sets the total number of pages available for pagination.
    /// </summary>
    public int PageCount { get; set; } = 1;
    
    /// <summary>
    /// Gets or sets the current state of the table including sorting, filtering, and pagination.
    /// </summary>
    public TableState State { get; set; } = new();
    
    /// <summary>
    /// Gets or sets the model handler responsible for data operations.
    /// </summary>
    public ModelHandler<T, TKey> ModelHandler { get; set; } = default!;
    
    /// <summary>
    /// Gets or sets a collection of factory functions that generate action models for the table.
    /// </summary>
    public List<Func<TableModel<T, TKey>, Task<IEnumerable<ActionModel>>>> ActionsFactories { get; set; } = [];
    
    internal Expression<Func<T, TKey>>? KeySelector { get; set; } = default!;

    /// <summary>
    /// Initializes a new instance of the TableModel class with the specified configuration.
    /// </summary>
    /// <param name="config">The configuration settings for the table model.</param>
    public TableModel(TableModelConfig<T, TKey> config)
    {
        TypeId = config.TypeId ?? typeof(T).Name;
        Columns = config.Columns;
        ModelHandler = config.ModelHandler ?? throw new ArgumentNullException(nameof(config.ModelHandler));
        ActionsFactories = config.ActionsFactories;
        if (config.KeySelector != null)
            KeySelector = config.KeySelector;
    }

    ModelHandler ITableModel.ModelHandler
    {
        get => ModelHandler;
        set => ModelHandler = (ModelHandler<T, TKey>)value;
    }

    // Explicit implementation of ITableModel
    List<ITableRowContext> ITableModel.Rows
    {
        get => Rows.Cast<ITableRowContext>().ToList();
        set => Rows = value.Cast<TableRowContext<T, TKey>>().ToList();
    }

    List<ITableColumnModel> ITableModel.Columns
    {
        get => Columns.Cast<ITableColumnModel>().ToList();
        set => Columns = value.Cast<TableColumnModel<T, TKey>>().ToList();
    }

    /// <summary>
    /// Gets the available actions for the table.
    /// </summary>
    /// <returns>A collection of action models for the table.</returns>
    public async Task<IEnumerable<ActionModel>> GetActionsAsync()
    {
        var results = new List<ActionModel>();
        foreach (var factory in ActionsFactories)
        {
            var actions = await factory(this);
            if (actions != null)
                results.AddRange(actions);
        }
        return results;
    }
}

/// <summary>
/// Configuration class for building table models.
/// </summary>
/// <typeparam name="T">The entity type displayed in the table.</typeparam>
/// <typeparam name="TKey">The key type for the entity.</typeparam>
public class TableModelConfig<T, TKey>
    where T : class
{
    /// <summary>
    /// Gets or sets the unique type identifier for the table model.
    /// </summary>
    public string? TypeId { get; set; }
    
    /// <summary>
    /// Gets or sets the key selector expression for the entity.
    /// </summary>
    public Expression<Func<T, TKey>>? KeySelector { get; set; }
    
    /// <summary>
    /// Gets or sets the model handler responsible for data operations.
    /// </summary>
    public ModelHandler<T, TKey>? ModelHandler { get; set; }
    
    /// <summary>
    /// Gets the collection of column models defining the table structure.
    /// </summary>
    public List<TableColumnModel<T, TKey>> Columns { get; } = new();
    
    /// <summary>
    /// Gets or sets a collection of factory functions that generate action models for the table.
    /// </summary>
    public List<Func<TableModel<T, TKey>, Task<IEnumerable<ActionModel>>>> ActionsFactories { get; set; } = [];
}

/// <summary>
/// Represents a placeholder type for entities that don't have a meaningful key.
/// </summary>
public sealed class NoKey
{
    // This class is used as a placeholder for models that do not have a key.
    // It can be used in scenarios where the model does not require a key, such as for readonly reports.
}