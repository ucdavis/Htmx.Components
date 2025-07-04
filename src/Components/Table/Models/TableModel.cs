using System.Linq.Expressions;
using Htmx.Components.Models;

namespace Htmx.Components.Table.Models;

// We have a separate, non-generic class, since razor views don't support generic type params
public interface ITableModel
{
    public string TypeId { get; set; }
    public List<ITableRowContext> Rows { get; set; }
    public List<ITableColumnModel> Columns { get; set; }
    public int PageCount { get; set; }
    public TableState State { get; set; }
    public ModelHandler ModelHandler { get; set; }
    public Task<IEnumerable<ActionModel>> GetActionsAsync();
}


public class TableModel<T, TKey> : ITableModel
    where T : class
{
    public string TypeId { get; set; } = typeof(T).Name;
    public List<TableRowContext<T, TKey>> Rows { get; set; } = new();
    public List<TableColumnModel<T, TKey>> Columns { get; set; } = new();
    public int PageCount { get; set; } = 1;
    public TableState State { get; set; } = new();
    public ModelHandler<T, TKey> ModelHandler { get; set; } = default!;
    public List<Func<TableModel<T, TKey>, Task<IEnumerable<ActionModel>>>> ActionsFactories { get; set; } = [];
    internal Expression<Func<T, TKey>>? KeySelector { get; set; } = default!;

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

public class TableModelConfig<T, TKey>
    where T : class
{
    public string? TypeId { get; set; }
    public Expression<Func<T, TKey>>? KeySelector { get; set; }
    public ModelHandler<T, TKey>? ModelHandler { get; set; }
    public List<TableColumnModel<T, TKey>> Columns { get; } = new();
    public List<Func<TableModel<T, TKey>, Task<IEnumerable<ActionModel>>>> ActionsFactories { get; set; } = [];
}


public sealed class NoKey
{
    // This class is used as a placeholder for models that do not have a key.
    // It can be used in scenarios where the model does not require a key, such as for readonly reports.
}