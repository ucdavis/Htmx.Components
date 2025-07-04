using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Htmx.Components.AuthStatus.Models;

namespace Htmx.Components.AuthStatus;

/// <summary>
/// Default implementation of <see cref="IAuthStatusProvider"/> that provides basic authentication status information.
/// </summary>
/// <remarks>
/// This implementation extracts user information from the <see cref="ClaimsPrincipal"/> and provides
/// default values for profile images and login URLs. Applications can replace this with custom
/// implementations to integrate with specific authentication systems or user stores.
/// </remarks>
public class DefaultAuthStatusProvider : IAuthStatusProvider
{
    /// <summary>
    /// Gets authentication status information for the specified user.
    /// </summary>
    /// <param name="user">The user principal to get status information for.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the authentication status view model.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="user"/> is null.</exception>
    public Task<AuthStatusViewModel> GetAuthStatusAsync(ClaimsPrincipal user)
    {
        if (user == null) throw new ArgumentNullException(nameof(user));

        var isAuthenticated = user.Identity?.IsAuthenticated ?? false;
        return Task.FromResult(new AuthStatusViewModel
        {
            IsAuthenticated = isAuthenticated,
            UserName = isAuthenticated ? user.Identity?.Name : null,
            ProfileImageUrl = null, // No image by default
            LoginUrl = "/Auth/Login" // Default login route
        });
    }
}