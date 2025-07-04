using Htmx.Components.Models;
using Htmx.Components.Models.Builders;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;

namespace Htmx.Components.NavBar;

/// <summary>
/// Provides navigation functionality for building navigation action sets.
/// </summary>
/// <remarks>
/// Navigation providers are responsible for building the navigation structure
/// that will be displayed by navigation view components. Different implementations
/// can provide navigation through different mechanisms such as attributes,
/// configuration, or programmatic builders.
/// </remarks>
public interface INavProvider
{
    /// <summary>
    /// Builds the navigation action set for the current request context.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation. The task result contains the navigation action set.</returns>
    Task<IActionSet> BuildAsync();
}

/// <summary>
/// A navigation provider that builds its navigation action set via a given builder factory delegate.
/// </summary>
/// <remarks>
/// This provider is useful when you want to programmatically define navigation structure
/// rather than using attributes. The builder factory is called each time navigation
/// is requested, allowing for dynamic navigation based on current context.
/// </remarks>
/// <example>
/// Configure programmatic navigation during service registration:
/// <code>
/// services.AddHtmxComponents(options =>
/// {
///     options.WithNavBuilder(async builder =>
///     {
///         builder.AddAction(action => action
///             .WithLabel("Dashboard")
///             .WithIcon("fas fa-dashboard")
///             .WithHxGet("/Dashboard"));
///             
///         builder.AddGroup(group =>
///         {
///             group.WithLabel("Administration");
///             group.AddAction(action => action.WithLabel("Users").WithHxGet("/Admin/Users"));
///         });
///     });
/// });
/// </code>
/// </example>
public class BuilderBasedNavProvider : INavProvider
{
    private readonly Func<ActionSetBuilder, Task> _builderFactory;
    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="BuilderBasedNavProvider"/> class.
    /// </summary>
    /// <param name="serviceProvider">The service provider for dependency resolution.</param>
    /// <param name="builderFactory">The factory function that configures the navigation builder.</param>
    /// <exception cref="ArgumentNullException">Thrown when any parameter is null.</exception>
    public BuilderBasedNavProvider(IServiceProvider serviceProvider, Func<ActionSetBuilder, Task> builderFactory)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _builderFactory = builderFactory ?? throw new ArgumentNullException(nameof(builderFactory));
    }

    /// <summary>
    /// Builds the navigation action set using the configured builder factory.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation. The task result contains the navigation action set.</returns>
    public async Task<IActionSet> BuildAsync()
    {
        // Call the async factory to build the ActionSet
        var actionSetBuilder = new ActionSetBuilder(_serviceProvider);
        await _builderFactory(actionSetBuilder);

        // Build the final ActionSet
        return await actionSetBuilder.BuildAsync();
    }
}