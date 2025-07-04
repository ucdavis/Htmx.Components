using System.Linq.Expressions;
using Htmx.Components.Authorization;
using Htmx.Components.Extensions;
using Htmx.Components.Models;
using Htmx.Components.Table.Models;
using Htmx.Components.State;
using Htmx.Components.Table;
using Microsoft.Extensions.DependencyInjection;
using static Htmx.Components.Authorization.AuthConstants;

namespace Htmx.Components.Models.Builders;

/// <summary>
/// Provides a fluent API for configuring and building model handlers that manage CRUD operations and UI generation.
/// This builder configures data access, business logic delegates, table presentation, and input models for a specific entity type.
/// </summary>
/// <typeparam name="T">The model type being handled, must be a reference type with a parameterless constructor</typeparam>
/// <typeparam name="TKey">The type of the model's primary key</typeparam>
public class ModelHandlerBuilder<T, TKey> : BuilderBase<ModelHandlerBuilder<T, TKey>, ModelHandler<T, TKey>>
    where T : class, new()
{
    private readonly IResourceOperationRegistry _resourceOperationRegistry;
    private readonly ModelHandlerOptions<T, TKey> _options = new();
    private readonly ITableProvider _tableProvider;
    private readonly IPageState _pageState;

    internal ModelHandlerBuilder(IServiceProvider serviceProvider, string typeId, IResourceOperationRegistry resourceOperationRegistry)
        : base(serviceProvider)
    {
        _resourceOperationRegistry = resourceOperationRegistry;
        _options.TypeId = typeId;
        _options.ModelUI = ModelUI.Table;
        _options.ServiceProvider = serviceProvider;
        _tableProvider = serviceProvider.GetRequiredService<ITableProvider>();
        _pageState = serviceProvider.GetRequiredService<IPageState>();
    }

    /// <summary>
    /// Sets the unique type identifier for this model handler.
    /// This identifier is used for routing, authorization, and UI component registration.
    /// </summary>
    /// <param name="typeId">A unique string identifier for the model type</param>
    /// <returns>The current builder instance for method chaining</returns>
    public ModelHandlerBuilder<T, TKey> WithTypeId(string typeId)
    {
        _options.TypeId = typeId;
        return this;
    }

    /// <summary>
    /// Configures the key selector expression that identifies the primary key property or properties of the model.
    /// This expression is used for entity identification, filtering, and CRUD operations.
    /// </summary>
    /// <param name="keySelector">An expression that selects the primary key from the model</param>
    /// <returns>The current builder instance for method chaining</returns>
    public ModelHandlerBuilder<T, TKey> WithKeySelector(Expression<Func<T, TKey>> keySelector)
    {
        _options.KeySelector = keySelector;
        return this;
    }

    /// <summary>
    /// Configures the queryable data source for read operations.
    /// This delegate provides the base query for retrieving entities and enables read CRUD functionality.
    /// Also registers the read operation with the authorization system.
    /// </summary>
    /// <param name="getQueryable">A function that returns the queryable data source for the model</param>
    /// <returns>The current builder instance for method chaining</returns>
    public ModelHandlerBuilder<T, TKey> WithQueryable(Func<IQueryable<T>> getQueryable)
    {
        _options.Crud.CrudFeatures |= CrudFeatures.Read;
        _options.Crud.GetQueryable = getQueryable;
        AddBuildTask(_resourceOperationRegistry.Register(_options.TypeId!, CrudOperations.Read));
        return this;
    }

    /// <summary>
    /// Configures the create operation for adding new entities.
    /// This enables create CRUD functionality and sets up the associated action models for UI generation.
    /// Also registers the create operation with the authorization system.
    /// </summary>
    /// <param name="createModel">A function that creates a new entity and returns the result</param>
    /// <returns>The current builder instance for method chaining</returns>
    public ModelHandlerBuilder<T, TKey> WithCreate(Func<T, Task<Result<T>>> createModel)
    {
        _options.Crud.CrudFeatures |= CrudFeatures.Create;
        _options.Crud.CreateModel = createModel;
        _options.Crud.GetCreateActionModel = () => new ActionModel(new ActionModelConfig
        {
            Label = "Create",
            Icon = "fas fa-plus mr-1",
            Attributes = new Dictionary<string, string>
            {
                { "hx-post", $"/Form/{_options.TypeId}/{_options.ModelUI}/Create" },
            }
        });
        SetCancelActionModel();
        AddBuildTask(_resourceOperationRegistry.Register(_options.TypeId!, CrudOperations.Create));
        return this;
    }

    /// <summary>
    /// Configures the update operation for modifying existing entities.
    /// This enables update CRUD functionality and sets up the associated action models for UI generation.
    /// Also registers the update operation with the authorization system.
    /// </summary>
    /// <param name="updateModel">A function that updates an existing entity and returns the result</param>
    /// <returns>The current builder instance for method chaining</returns>
    public ModelHandlerBuilder<T, TKey> WithUpdate(Func<T, Task<Result<T>>> updateModel)
    {
        _options.Crud.CrudFeatures |= CrudFeatures.Update;
        _options.Crud.UpdateModel = updateModel;
        _options.Crud.GetUpdateActionModel = () => new ActionModel(new ActionModelConfig
        {
            Label = "Update",
            Icon = "fas fa-edit mr-1",
            Attributes = new Dictionary<string, string>
            {
                { "hx-post", $"/Form/{_options.TypeId}/{_options.ModelUI}/Update" },
            }
        });
        SetCancelActionModel();
        AddBuildTask(_resourceOperationRegistry.Register(_options.TypeId!, CrudOperations.Update));
        return this;
    }

    private void SetCancelActionModel()
    {
        if (_options.Crud.GetCancelActionModel != null)
            return;
        _options.Crud.GetCancelActionModel = () => new ActionModel(new ActionModelConfig
        {
            Label = "Cancel",
            Icon = "fas fa-times mr-1",
            Attributes = new Dictionary<string, string>
            {
                { "hx-get", $"/Form/{_options.TypeId}/{_options.ModelUI}/Cancel" },
            }
        });
    }

    /// <summary>
    /// Configures the delete operation for removing entities.
    /// This enables delete CRUD functionality and sets up the associated action models for UI generation.
    /// Also registers the delete operation with the authorization system.
    /// </summary>
    /// <param name="deleteModel">A function that deletes an entity by its key and returns the result</param>
    /// <returns>The current builder instance for method chaining</returns>
    public ModelHandlerBuilder<T, TKey> WithDelete(Func<TKey, Task<Result>> deleteModel)
    {
        _options.Crud.CrudFeatures |= CrudFeatures.Delete;
        _options.Crud.DeleteModel = deleteModel;
        _options.Crud.GetDeleteActionModel = () => new ActionModel(new ActionModelConfig
        {
            Label = "Delete",
            Icon = "fas fa-trash mr-1",
            Attributes = new Dictionary<string, string>
            {
                { "hx-delete", $"/Form/{_options.TypeId}/{_options.ModelUI}/Delete" },
            }
        });
        AddBuildTask(_resourceOperationRegistry.Register(_options.TypeId!, CrudOperations.Delete));
        return this;
    }

    /// <summary>
    /// Configures the table model builder that defines how the entities are displayed in tabular format.
    /// This allows customization of columns, actions, filtering, and other table-specific settings.
    /// </summary>
    /// <param name="configure">An action that configures the table model builder</param>
    /// <returns>The current builder instance for method chaining</returns>
    public ModelHandlerBuilder<T, TKey> WithTable(Action<TableModelBuilder<T, TKey>> configure)
    {
        _options.Table.ConfigureTableModel = configure;
        return this;
    }

    /// <summary>
    /// Configures an input model for a specific property of the entity.
    /// Input models define how properties are edited in forms, including validation, UI controls, and behavior.
    /// </summary>
    /// <typeparam name="TProp">The type of the property being configured</typeparam>
    /// <param name="propertySelector">An expression that selects the property to configure</param>
    /// <param name="configure">An action that configures the input model builder for the property</param>
    /// <returns>The current builder instance for method chaining</returns>
    public ModelHandlerBuilder<T, TKey> WithInput<TProp>(Expression<Func<T, TProp>> propertySelector,
        Action<InputModelBuilder<T, TProp>> configure)
    {
        _options.Inputs.InputModelBuilders.TryAdd(propertySelector.GetPropertyName(), async (modelHandler) =>
        {
            var builder = new InputModelBuilder<T, TProp>(ServiceProvider, propertySelector);
            configure(builder);
            var inputModel = await builder.BuildAsync();
            inputModel.ModelHandler = modelHandler;
            return inputModel;
        });
        return this;
    }

    /// <summary>
    /// Builds the configured model handler instance.
    /// This method is called internally by the builder framework to create the final model handler.
    /// </summary>
    /// <returns>A task that represents the asynchronous build operation. The task result contains the configured model handler.</returns>
    protected override Task<ModelHandler<T, TKey>> BuildImpl()
    {
        var handler = new ModelHandler<T, TKey>(_options, _tableProvider, _pageState);
        return Task.FromResult(handler);
    }

}
