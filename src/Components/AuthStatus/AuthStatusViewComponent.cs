using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using Htmx.Components.Models;

namespace Htmx.Components.AuthStatus;

/// <summary>
/// View component that renders authentication status information for the current user.
/// </summary>
/// <remarks>
/// This component displays different content based on whether the user is authenticated,
/// including user information, profile images, and login/logout links. The actual content
/// and behavior are determined by the registered <see cref="IAuthStatusProvider"/>.
/// </remarks>
public class AuthStatusViewComponent : ViewComponent
{
    private readonly IAuthStatusProvider _authStatusProvider;
    private readonly ViewPaths _viewPaths;

    /// <summary>
    /// Initializes a new instance of the <see cref="AuthStatusViewComponent"/> class.
    /// </summary>
    /// <param name="authStatusProvider">The provider that generates authentication status data.</param>
    /// <param name="viewPaths">The configured view paths for rendering components.</param>
    /// <exception cref="ArgumentNullException">Thrown when any parameter is null.</exception>
    public AuthStatusViewComponent(IAuthStatusProvider authStatusProvider, ViewPaths viewPaths)
    {
        _authStatusProvider = authStatusProvider ?? throw new ArgumentNullException(nameof(authStatusProvider));
        _viewPaths = viewPaths ?? throw new ArgumentNullException(nameof(viewPaths));
    }

    /// <summary>
    /// Invokes the view component to render authentication status.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation. The task result contains the view component result.</returns>
    public async Task<IViewComponentResult> InvokeAsync()
    {
        var model = await _authStatusProvider.GetAuthStatusAsync(HttpContext.User);
        return View(_viewPaths.AuthStatus, model);
    }
}