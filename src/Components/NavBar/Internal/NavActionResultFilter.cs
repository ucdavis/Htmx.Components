using Htmx.Components.Filters;
using Htmx.Components.Models;
using Htmx.Components.NavBar;
using Htmx.Components.ViewResults;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Htmx.Components.NavBar.Internal;

/// <summary>
/// A result filter that processes controller actions marked with <see cref="NavActionAttribute"/>
/// to provide navigation-aware responses. This filter updates the navigation bar and renders
/// the appropriate content based on whether the request is an HTMX request or a full page request.
/// </summary>
/// <remarks>
/// For HTMX requests, this filter performs out-of-band updates to refresh the navigation bar
/// and renders the action's content in the configured view. For non-HTMX requests, it renders
/// the full page using the view specified in the NavActionAttribute.
/// </remarks>
public class NavActionResultFilter : OobResultFilterBase<NavActionAttribute>
{
    private readonly INavProvider _navProvider;
    private readonly ViewPaths _viewPaths;

    /// <summary>
    /// Initializes a new instance of the NavActionResultFilter class.
    /// </summary>
    /// <param name="navProvider">The navigation provider for building updated navigation content.</param>
    /// <param name="viewPaths">The view paths configuration for resolving view names.</param>
    public NavActionResultFilter(INavProvider navProvider, ViewPaths viewPaths)
    {
        _navProvider = navProvider;
        _viewPaths = viewPaths;
    }

    /// <summary>
    /// Gets the view name to render for non-HTMX requests.
    /// Returns the view name specified in the NavActionAttribute, or null if not specified.
    /// </summary>
    /// <param name="attribute">The NavActionAttribute instance containing the view name.</param>
    /// <param name="cad">The controller action descriptor.</param>
    /// <returns>A task containing the view name, or null if no specific view is configured.</returns>
    protected override Task<string?> GetViewNameForNonHtmxRequest(NavActionAttribute attribute, ControllerActionDescriptor cad)
    {
        return Task.FromResult(attribute.ViewName);
    }

    /// <summary>
    /// Updates the MultiSwapViewResult with refreshed navigation content and the action's result.
    /// This method performs out-of-band updates for the navigation bar and renders the action content
    /// in the specified view or the default navigation content view.
    /// </summary>
    /// <param name="attribute">The NavActionAttribute that triggered this filter.</param>
    /// <param name="multiSwapViewResult">The MultiSwapViewResult to update with navigation and content.</param>
    /// <param name="context">The result executing context.</param>
    /// <returns>A task representing the asynchronous update operation.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the MultiSwapViewResult has no model set.</exception>
    protected override async Task UpdateMultiSwapViewResultAsync(NavActionAttribute attribute,
        MultiSwapViewResult multiSwapViewResult, ResultExecutingContext context)
    {
        if (multiSwapViewResult.Model == null)
        {
            throw new InvalidOperationException($"MultiSwapViewResult must have a model set when filtering via {nameof(NavActionAttribute)}.");
        }
        var navbar = await _navProvider.BuildAsync();

        multiSwapViewResult
            .WithOobContent(ComponentNames.NavBar, navbar)
            .WithOobContent(attribute.ViewName ?? _viewPaths.DefaultNavContent, multiSwapViewResult.Model);
    }
}