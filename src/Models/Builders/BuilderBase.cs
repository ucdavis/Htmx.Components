using Htmx.Components.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace Htmx.Components.Models.Builders;

/// <summary>
/// Abstract base class for fluent builders that create model instances.
/// Provides common functionality including dependency injection, build task management, and action context access.
/// </summary>
/// <typeparam name="TBuilder">The concrete builder type for fluent method chaining</typeparam>
/// <typeparam name="TModel">The type of model being built</typeparam>
public abstract class BuilderBase<TBuilder, TModel>
    where TBuilder : BuilderBase<TBuilder, TModel>
    where TModel : class
{
    /// <summary>
    /// Gets the service provider for dependency injection.
    /// Used to resolve services needed during the build process.
    /// </summary>
    public IServiceProvider ServiceProvider { get; }
    private readonly List<Func<Task>> _buildTasks;

    /// <summary>
    /// Initializes a new instance of the BuilderBase with the specified service provider.
    /// </summary>
    /// <param name="serviceProvider">The service provider for dependency injection</param>
    protected BuilderBase(IServiceProvider serviceProvider)
    {
        ServiceProvider = serviceProvider;
        _buildTasks = [];
    }

    /// <summary>
    /// Gets the current action context for the request.
    /// Provides access to request information, routing data, and action metadata.
    /// </summary>
    public ActionContext ActionContext
    {
        get
        {
            var actionContextAccessor = ServiceProvider.GetRequiredService<IActionContextAccessor>();
            return actionContextAccessor.GetValidActionContext();
        }
    }

    /// <summary>
    /// Adds a build task to be executed during the build process.
    /// Build tasks allow for asynchronous operations to be performed when creating the model.
    /// </summary>
    /// <param name="task">The task to execute during building</param>
    protected void AddBuildTask(Task task)
    {
        _buildTasks.Add(() => task);
    }

    /// <summary>
    /// Adds a build task from a function to be executed during the build process.
    /// Build tasks allow for asynchronous operations to be performed when creating the model.
    /// </summary>
    /// <param name="taskFactory">A function that returns the task to execute during building</param>
    protected void AddBuildTask(Func<Task> taskFactory)
    {
        _buildTasks.Add(taskFactory);
    }

    /// <summary>
    /// Adds a synchronous build action to be executed during the build process.
    /// The action will be wrapped in a task for consistent execution.
    /// </summary>
    /// <param name="action">The action to execute during building</param>
    protected void AddBuildTask(Action action)
    {
        _buildTasks.Add(() => { action(); return Task.CompletedTask; });
    }

    /// <summary>
    /// Builds the model instance by executing all build tasks and then calling the implementation-specific build logic.
    /// This method coordinates the entire build process and must be implemented by derived classes.
    /// </summary>
    /// <returns>A task that represents the asynchronous build operation, containing the built model</returns>
    protected abstract Task<TModel> BuildImpl();

    internal async Task<TModel> BuildAsync()
    {
        var tasks = _buildTasks.Select(f => f());
        await Task.WhenAll(tasks);
        return await BuildImpl();
    }
}
