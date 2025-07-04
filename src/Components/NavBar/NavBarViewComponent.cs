using Htmx.Components.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Htmx.Components.NavBar;

/// <summary>
/// View component that renders the navigation bar using the configured navigation provider.
/// </summary>
/// <remarks>
/// This component delegates to the registered <see cref="INavProvider"/> to build
/// the navigation structure and then renders it using the configured view.
/// The navigation is automatically filtered based on the current user's permissions.
/// </remarks>
public class NavBarViewComponent : ViewComponent
{
    private readonly INavProvider _navProvider;
    private readonly ViewPaths _viewPaths;

    /// <summary>
    /// Initializes a new instance of the <see cref="NavBarViewComponent"/> class.
    /// </summary>
    /// <param name="navProvider">The navigation provider that builds the navigation structure.</param>
    /// <param name="viewPaths">The configured view paths for rendering components.</param>
    /// <exception cref="ArgumentNullException">Thrown when any parameter is null.</exception>
    public NavBarViewComponent(INavProvider navProvider, ViewPaths viewPaths)
    {
        _navProvider = navProvider ?? throw new ArgumentNullException(nameof(navProvider));
        _viewPaths = viewPaths ?? throw new ArgumentNullException(nameof(viewPaths));
    }

    /// <summary>
    /// Invokes the view component to render the navigation bar.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation. The task result contains the view component result.</returns>
    public async Task<IViewComponentResult> InvokeAsync()
    {
        var model = await _navProvider.BuildAsync();
        return View(_viewPaths.NavBar, model);
    }
}