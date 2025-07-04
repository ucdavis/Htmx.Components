using System.Linq.Expressions;
using FastExpressionCompiler;
using Htmx.Components.Table;
using Htmx.Components.Table.Models;
using Htmx.Components.Models.Builders;
using Htmx.Components.State;
using static Htmx.Components.State.PageStateConstants;

namespace Htmx.Components.Models;

public abstract class ModelHandler
{
    public string TypeId { get; set; } = null!;
    public Type ModelType { get; protected set; } = null!;
    public Type KeyType { get; protected set; } = null!;
    public CrudFeatures CrudFeatures { get; internal set; }
    public ModelUI ModelUI { get; set; }
}

public enum ModelUI
{
    Table,
}

public class ModelHandler<T, TKey> : ModelHandler
    where T : class
{
    private Expression<Func<T, TKey>> _keySelectorExpression = null!;
    private Func<T, TKey> _keySelectorFunc = null!;
    private ITableProvider _tableProvider;
    private IPageState _pageState;

    internal ModelHandler(ModelHandlerOptions<T, TKey> options, ITableProvider tableProvider, IPageState pageState)
    {
        _tableProvider = tableProvider;
        _pageState = pageState;
        if (options.TypeId == null) throw new ArgumentNullException(nameof(options.TypeId));
        if (options.ServiceProvider == null) throw new ArgumentNullException(nameof(options.ServiceProvider));

        TypeId = options.TypeId;
        ModelType = typeof(T);
        KeyType = typeof(TKey);
        ModelUI = options.ModelUI;
        ServiceProvider = options.ServiceProvider;

        if (options.KeySelector != null)
            KeySelector = options.KeySelector;

        // CRUD
        CrudFeatures = options.Crud.CrudFeatures;
        GetQueryable = options.Crud.GetQueryable;
        CreateModel = options.Crud.CreateModel;
        UpdateModel = options.Crud.UpdateModel;
        DeleteModel = options.Crud.DeleteModel;
        GetCreateActionModel = options.Crud.GetCreateActionModel;
        GetUpdateActionModel = options.Crud.GetUpdateActionModel;
        GetCancelActionModel = options.Crud.GetCancelActionModel;
        GetDeleteActionModel = options.Crud.GetDeleteActionModel;

        // Table
        ConfigureTableModel = options.Table.ConfigureTableModel;

        // Inputs
        InputModelBuilders = options.Inputs.InputModelBuilders;
    }

    public Expression<Func<T, TKey>> KeySelector
    {
        get => _keySelectorExpression;
        set
        {
            _keySelectorExpression = value;
            _keySelectorFunc = value.CompileFast();
        }
    }

    internal Func<IQueryable<T>>? GetQueryable { get; set; }
    internal Func<T, Task<Result<T>>>? CreateModel { get; set; }
    internal Func<T, Task<Result<T>>>? UpdateModel { get; set; }
    internal Func<TKey, Task<Result>>? DeleteModel { get; set; }
    internal Func<ActionModel>? GetCreateActionModel { get; set; }
    internal Func<ActionModel>? GetUpdateActionModel { get; set; }
    internal Func<ActionModel>? GetCancelActionModel { get; set; }
    internal Func<ActionModel>? GetDeleteActionModel { get; set; }
    internal Func<T, TKey> KeySelectorFunc => _keySelectorFunc;
    internal Dictionary<string, Func<ModelHandler<T, TKey>, Task<IInputModel>>>? InputModelBuilders { get; set; }
    internal Action<TableModelBuilder<T, TKey>>? ConfigureTableModel { get; set; }
    internal IServiceProvider ServiceProvider { get; set; } = null!;

    public Task<TableModel<T, TKey>> BuildTableModelAsync()
    {
        var tableModelBuilder = new TableModelBuilder<T, TKey>(_keySelectorExpression, this, ServiceProvider);
        ConfigureTableModel?.Invoke(tableModelBuilder);
        return tableModelBuilder.BuildAsync();
    }

    public async Task<TableModel<T, TKey>> BuildTableModelAndFetchPageAsync(TableState? tableState = null)
    {
        // a null tableState means we are opening a new table with no previous state.
        if (tableState == null)
        {
            tableState = new TableState();
            _pageState.Set(TableStateKeys.Partition, TableStateKeys.TableState, tableState);
        }

        var tableModelBuilder = new TableModelBuilder<T, TKey>(_keySelectorExpression, this, ServiceProvider);
        ConfigureTableModel?.Invoke(tableModelBuilder);
        var tableModel = await tableModelBuilder.BuildAsync();
        var query = GetQueryable?.Invoke() ?? throw new InvalidOperationException("GetQueryable is not set.");
        await _tableProvider.FetchPageAsync(tableModel, query, tableState);
        return tableModel;
    }

    public async Task<IInputModel> BuildInputModel(string name)
    {
        if (InputModelBuilders == null || !InputModelBuilders.TryGetValue(name, out var builder))
            throw new ArgumentException($"No input model found for name '{name}'.");

        return await builder(this);
    }

    /// <summary>
    /// Creates a predicate expression for the key selector. This is used to filter a collection
    /// to a single item based on the key. The key can be a simple value type, a string, or a
    /// composite type (e.g., a tuple or a class).
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    /// <exception cref="NotSupportedException"></exception>
    public Expression<Func<T, bool>> GetKeyPredicate(TKey key)
    {
        var param = KeySelector.Parameters[0];
        Expression body;

        if (typeof(TKey).IsValueType || typeof(TKey) == typeof(string))
        {
            // Simple key: x => KeySelector(x) == key
            body = Expression.Equal(KeySelector.Body, Expression.Constant(key, typeof(TKey)));
        }
        else if (typeof(TKey).IsGenericType && typeof(TKey).Name.StartsWith("ValueTuple"))
        {
            // Composite key: compare each field
            var comparisons = new List<Expression>();
            var fields = typeof(TKey).GetFields();
            for (int i = 0; i < fields.Length; i++)
            {
                var memberAccess = Expression.Field(KeySelector.Body, fields[i]);
                var keyValue = Expression.Constant(fields[i].GetValue(key), fields[i].FieldType);
                comparisons.Add(Expression.Equal(memberAccess, keyValue));
            }
            body = comparisons.Aggregate(Expression.AndAlso);
        }
        else if (typeof(TKey).IsClass)
        {
            // Composite key: compare each property
            var comparisons = new List<Expression>();
            var properties = typeof(TKey).GetProperties();
            foreach (var prop in properties)
            {
                var memberAccess = Expression.Property(KeySelector.Body, prop);
                var keyValue = Expression.Constant(prop.GetValue(key), prop.PropertyType);
                comparisons.Add(Expression.Equal(memberAccess, keyValue));
            }
            if (comparisons.Count == 0)
                throw new NotSupportedException($"Key type {typeof(TKey)} has no properties.");
            body = comparisons.Aggregate(Expression.AndAlso);
        }
        else
        {
            throw new NotSupportedException($"Key type {typeof(TKey)} is not supported.");
        }

        return Expression.Lambda<Func<T, bool>>(body, param);
    }
}

[Flags]
public enum CrudFeatures
{
    None = 0,
    Create = 1,
    Read = 2,
    Update = 4,
    Delete = 8
}

/// <summary>
/// Internal options class used by the framework to store CRUD operation configuration.
/// This class should not be used directly in user code.
/// </summary>
/// <remarks>
/// This class contains delegates and configuration for create, read, update, and delete operations
/// that are configured through the model handler builder pattern.
/// </remarks>
internal class CrudOptions<T, TKey>
{
    public Func<IQueryable<T>>? GetQueryable { get; set; }
    public Func<T, Task<Result<T>>>? CreateModel { get; set; }
    public Func<T, Task<Result<T>>>? UpdateModel { get; set; }
    public Func<TKey, Task<Result>>? DeleteModel { get; set; }
    public CrudFeatures CrudFeatures { get; set; }
    public Func<ActionModel>? GetCreateActionModel { get; set; }
    public Func<ActionModel>? GetUpdateActionModel { get; set; }
    public Func<ActionModel>? GetCancelActionModel { get; set; }
    public Func<ActionModel>? GetDeleteActionModel { get; set; }
}

/// <summary>
/// Internal options class used by the framework to store table-specific configuration.
/// This class should not be used directly in user code.
/// </summary>
/// <remarks>
/// This class contains table model building configuration and view path information
/// used during table model construction.
/// </remarks>
internal class TableOptions<T, TKey>
    where T : class
{
    public Action<TableModelBuilder<T, TKey>>? ConfigureTableModel { get; set; }
}

/// <summary>
/// Internal options class used by the framework to store input model configuration.
/// This class should not be used directly in user code.
/// </summary>
/// <remarks>
/// This class contains input model builders and related configuration used
/// for form field generation and editing.
/// </remarks>
internal class InputOptions<T, TKey>
    where T : class
{
    public Dictionary<string, Func<ModelHandler<T, TKey>, Task<IInputModel>>> InputModelBuilders { get; } = new();
}

internal class ModelHandlerOptions<T, TKey>
    where T : class
{
    public string? TypeId { get; set; }
    public Expression<Func<T, TKey>>? KeySelector { get; set; }
    public CrudOptions<T, TKey> Crud { get; set; } = new();
    public TableOptions<T, TKey> Table { get; set; } = new();
    public InputOptions<T, TKey> Inputs { get; set; } = new();
    public IServiceProvider? ServiceProvider { get; set; }
    public ModelUI ModelUI { get; set; } = ModelUI.Table;
}