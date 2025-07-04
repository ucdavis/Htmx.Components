using System.Linq.Expressions;
using Htmx.Components.Authorization;
using Htmx.Components.Extensions;
using Htmx.Components.Input;
using Htmx.Components.Models;
using Htmx.Components.Models.Builders;
using Htmx.Components.Table.Models;
using Htmx.Components.Table;
using Microsoft.Extensions.DependencyInjection;

namespace Htmx.Components.Services;

/// <summary>
/// Provides a contract for registering and retrieving model handlers for different entity types.
/// Model handlers define how entities are displayed, edited, and managed in the UI.
/// </summary>
public interface IModelRegistry
{
    /// <summary>
    /// Registers a model handler for the specified entity type with configuration logic.
    /// </summary>
    /// <typeparam name="T">The entity type to register a handler for.</typeparam>
    /// <typeparam name="TKey">The type of the entity's primary key.</typeparam>
    /// <param name="typeId">A unique identifier for this model type registration.</param>
    /// <param name="config">A configuration action that sets up the model handler builder.</param>
    void Register<T, TKey>(string typeId, Action<IServiceProvider, ModelHandlerBuilder<T, TKey>> config)
        where T : class, new();
        
    /// <summary>
    /// Retrieves a model handler for the specified type identifier and UI configuration.
    /// </summary>
    /// <param name="typeId">The unique identifier for the model type.</param>
    /// <param name="modelUI">The UI configuration for the model.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the model handler.</returns>
    /// <exception cref="InvalidOperationException">Thrown when no model handler is found for the specified type identifier.</exception>
    Task<ModelHandler> GetModelHandler(string typeId, ModelUI modelUI);
    
    /// <summary>
    /// Retrieves a strongly-typed model handler for the specified type identifier and UI configuration.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <typeparam name="TKey">The type of the entity's primary key.</typeparam>
    /// <param name="typeId">The unique identifier for the model type.</param>
    /// <param name="modelUI">The UI configuration for the model.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the strongly-typed model handler.</returns>
    /// <exception cref="InvalidOperationException">Thrown when no model handler is found for the specified type identifier, or when the handler is not of the expected type.</exception>
    Task<ModelHandler<T, TKey>> GetModelHandler<T, TKey>(string typeId, ModelUI modelUI)
        where T : class;
}

/// <summary>
/// Implements model handler registration and retrieval functionality.
/// This class manages a registry of model handlers and provides lazy initialization of handlers when requested.
/// </summary>
public class ModelRegistry : IModelRegistry
{
    private readonly Dictionary<string, Task<ModelHandler>> _modelHandlers = new();
    private readonly IServiceProvider _serviceProvider;
    private readonly IResourceOperationRegistry _resourceOperationRegistry;

    /// <summary>
    /// Initializes a new instance of the ModelRegistry class.
    /// </summary>
    /// <param name="serviceProvider">The service provider for resolving dependencies.</param>
    /// <param name="resourceOperationRegistry">The registry for authorization resource operations.</param>
    public ModelRegistry(IServiceProvider serviceProvider,
        IResourceOperationRegistry resourceOperationRegistry)
    {
        _serviceProvider = serviceProvider;
        _resourceOperationRegistry = resourceOperationRegistry;
    }

    /// <summary>
    /// Registers a model handler for the specified entity type with configuration logic.
    /// The handler is built using the provided configuration and stored for later retrieval.
    /// </summary>
    /// <typeparam name="T">The entity type to register a handler for.</typeparam>
    /// <typeparam name="TKey">The type of the entity's primary key.</typeparam>
    /// <param name="typeId">A unique identifier for this model type registration.</param>
    /// <param name="config">A configuration action that sets up the model handler builder.</param>
    public void Register<T, TKey>(string typeId, Action<IServiceProvider, ModelHandlerBuilder<T, TKey>> config)
        where T : class, new()
    {
        var builder = new ModelHandlerBuilder<T, TKey>(_serviceProvider, typeId, _resourceOperationRegistry);
        config.Invoke(_serviceProvider, builder);
        var castTask = async () =>
        {
            var modelHandler = await builder.BuildAsync();
            return (ModelHandler)modelHandler;
        };
        _modelHandlers[typeId] = castTask();
    }

    /// <summary>
    /// Retrieves a model handler for the specified type identifier and UI configuration.
    /// </summary>
    /// <param name="typeId">The unique identifier for the model type.</param>
    /// <param name="modelUI">The UI configuration for the model.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the model handler.</returns>
    /// <exception cref="InvalidOperationException">Thrown when no model handler is found for the specified type identifier.</exception>
    public async Task<ModelHandler> GetModelHandler(string typeId, ModelUI modelUI)
    {
        var task = _modelHandlers.TryGetValue(typeId, out var handler) ? handler : null;
        if (task == null)
        {
            throw new InvalidOperationException($"Model handler for typeId '{typeId}' not found.");
        }
        var modelHandler = await task;
        modelHandler.ModelUI = modelUI;
        return modelHandler;
    }

    /// <summary>
    /// Retrieves a strongly-typed model handler for the specified type identifier and UI configuration.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <typeparam name="TKey">The type of the entity's primary key.</typeparam>
    /// <param name="typeId">The unique identifier for the model type.</param>
    /// <param name="modelUI">The UI configuration for the model.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the strongly-typed model handler.</returns>
    /// <exception cref="InvalidOperationException">Thrown when no model handler is found for the specified type identifier, or when the handler is not of the expected type.</exception>
    public async Task<ModelHandler<T, TKey>> GetModelHandler<T, TKey>(string typeId, ModelUI modelUI)
        where T : class
    {
        var handler = await GetModelHandler(typeId, modelUI);
        if (handler is ModelHandler<T, TKey> typedHandler)
        {
            return typedHandler;
        }
        throw new InvalidOperationException($"Model handler for typeId '{typeId}' is not of type '{typeof(ModelHandler<T, TKey>)}'.");
    }
}

/// <summary>
/// Factory interface for creating model handlers.
/// Inject this into your controllers rather than <see cref="IModelRegistry"/> in 
/// order to avoid DI recursion issues.
/// </summary>
public interface IModelHandlerFactory
{
    /// <summary>
    /// Creates a model handler for the specified type identifier and UI configuration.
    /// </summary>
    /// <param name="typeId">The unique identifier for the model type.</param>
    /// <param name="modelUI">The UI configuration for the model.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the model handler.</returns>
    Task<ModelHandler> Get(string typeId, ModelUI modelUI);
}

/// <summary>
/// Factory interface for creating strongly-typed model handlers.
/// Inject this into your controllers rather than <see cref="IModelRegistry"/> in 
/// order to avoid DI recursion issues.
/// </summary>
public interface IModelHandlerFactoryGeneric
{
    /// <summary>
    /// Creates a strongly-typed model handler for the specified type identifier and UI configuration.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <typeparam name="TKey">The type of the entity's primary key.</typeparam>
    /// <param name="typeId">The unique identifier for the model type.</param>
    /// <param name="modelUI">The UI configuration for the model.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the strongly-typed model handler.</returns>
    Task<ModelHandler<T, TKey>> Get<T, TKey>(string typeId, ModelUI modelUI)
        where T : class;
}

/// <summary>
/// Provides a factory interface for creating model handlers without direct dependency on the model registry.
/// This factory helps avoid dependency injection recursion issues when injecting into controllers.
/// </summary>
/// <remarks>
/// Controllers should inject this interface rather than <see cref="IModelRegistry"/> directly
/// to prevent circular dependency issues in the DI container.
/// </remarks>
public class ModelHandlerFactory : IModelHandlerFactory, IModelHandlerFactoryGeneric
{
    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// Initializes a new instance of the ModelHandlerFactory class.
    /// </summary>
    /// <param name="serviceProvider">The service provider for resolving the model registry.</param>
    public ModelHandlerFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    /// <summary>
    /// Creates a model handler for the specified type identifier and UI configuration.
    /// </summary>
    /// <param name="typeId">The unique identifier for the model type.</param>
    /// <param name="modelUI">The UI configuration for the model.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the model handler.</returns>
    public Task<ModelHandler> Get(string typeId, ModelUI modelUI)
    {
        var registry = _serviceProvider.GetRequiredService<IModelRegistry>();
        return registry.GetModelHandler(typeId, modelUI);
    }

    /// <summary>
    /// Creates a strongly-typed model handler for the specified type identifier and UI configuration.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <typeparam name="TKey">The type of the entity's primary key.</typeparam>
    /// <param name="typeId">The unique identifier for the model type.</param>
    /// <param name="modelUI">The UI configuration for the model.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the strongly-typed model handler.</returns>
    public Task<ModelHandler<T, TKey>> Get<T, TKey>(string typeId, ModelUI modelUI)
        where T : class
    {
        var registry = _serviceProvider.GetRequiredService<IModelRegistry>();
        return registry.GetModelHandler<T, TKey>(typeId, modelUI);
    }
}