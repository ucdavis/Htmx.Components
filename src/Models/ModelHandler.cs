using System.Linq.Expressions;
using FastExpressionCompiler;
using Htmx.Components.Table;
using Htmx.Components.Table.Models;
using Htmx.Components.Models.Builders;
using Htmx.Components.State;
using static Htmx.Components.State.PageStateConstants;

namespace Htmx.Components.Models;

/// <summary>
/// Abstract base class for model handlers that provide CRUD operations and UI generation.
/// Handles the coordination between data operations and UI components for specific model types.
/// </summary>
public abstract class ModelHandler
{
    /// <summary>
    /// Gets or sets the unique identifier for this model type.
    /// Used to identify and route requests to the appropriate handler.
    /// </summary>
    public string TypeId { get; set; } = null!;
    
    /// <summary>
    /// Gets the .NET type of the model this handler manages.
    /// Provides runtime type information for dynamic operations.
    /// </summary>
    public Type ModelType { get; protected set; } = null!;
    
    /// <summary>
    /// Gets the .NET type of the model's primary key.
    /// Used for type-safe key operations and routing.
    /// </summary>
    public Type KeyType { get; protected set; } = null!;
    
    /// <summary>
    /// Gets or sets the CRUD operations that this handler supports.
    /// Determines which actions are available in the UI and API.
    /// </summary>
    public CrudFeatures CrudFeatures { get; internal set; }
    
    /// <summary>
    /// Gets or sets the UI context for this model handler.
    /// Specifies how the model should be presented (Table, Form, etc.).
    /// </summary>
    public ModelUI ModelUI { get; set; }
}

/// <summary>
/// Specifies the user interface context for model presentation.
/// Determines which UI components and behaviors are used.
/// </summary>
public enum ModelUI
{
    /// <summary>
    /// Presents the model data in a tabular format with pagination, sorting, and filtering.
    /// </summary>
    Table,
}

/// <summary>
/// Strongly-typed model handler that provides CRUD operations and UI generation for a specific model type.
/// Coordinates between data access, business logic, and UI presentation.
/// </summary>
/// <typeparam name="T">The model type being handled</typeparam>
/// <typeparam name="TKey">The type of the model's primary key</typeparam>
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

    /// <summary>
    /// Gets or sets the key selector expression used to identify the primary key of the model.
    /// This expression is used for entity identification, filtering, and CRUD operations.
    /// </summary>
    /// <value>
    /// An expression that selects the primary key property or properties from the model type.
    /// The expression is compiled for performance when accessing key values.
    /// </value>
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

    /// <summary>
    /// Builds a table model for displaying the entities in a tabular format.
    /// Applies the configured table model settings without fetching data.
    /// </summary>
    /// <returns>
    /// A task that represents the asynchronous operation. 
    /// The task result contains a configured table model ready for data population.
    /// </returns>
    public Task<TableModel<T, TKey>> BuildTableModelAsync()
    {
        var tableModelBuilder = new TableModelBuilder<T, TKey>(_keySelectorExpression, this, ServiceProvider);
        ConfigureTableModel?.Invoke(tableModelBuilder);
        return tableModelBuilder.BuildAsync();
    }

    /// <summary>
    /// Builds a table model and fetches the requested page of data based on the table state.
    /// If no table state is provided, creates a new default table state and stores it in the page state.
    /// </summary>
    /// <param name="tableState">
    /// The current table state containing pagination, sorting, and filtering information.
    /// If null, a new default table state will be created and stored.
    /// </param>
    /// <returns>
    /// A task that represents the asynchronous operation.
    /// The task result contains a table model populated with the requested page of data.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the GetQueryable delegate is not configured.
    /// </exception>
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

    /// <summary>
    /// Builds an input model for the specified input name.
    /// Input models are used for form field generation and data binding.
    /// </summary>
    /// <param name="name">The name of the input model to build.</param>
    /// <returns>
    /// A task that represents the asynchronous operation.
    /// The task result contains the built input model.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// Thrown when no input model builder is found for the specified name.
    /// </exception>
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

/// <summary>
/// Defines the CRUD (Create, Read, Update, Delete) operations that a model handler supports.
/// These flags determine which actions are available in the user interface and API.
/// </summary>
[Flags]
public enum CrudFeatures
{
    /// <summary>
    /// No CRUD operations are supported.
    /// </summary>
    None = 0,
    
    /// <summary>
    /// Create operation is supported - allows adding new entities.
    /// </summary>
    Create = 1,
    
    /// <summary>
    /// Read operation is supported - allows viewing/listing entities.
    /// </summary>
    Read = 2,
    
    /// <summary>
    /// Update operation is supported - allows modifying existing entities.
    /// </summary>
    Update = 4,
    
    /// <summary>
    /// Delete operation is supported - allows removing entities.
    /// </summary>
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