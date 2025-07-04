using System.Security.Claims;

namespace Htmx.Components.Services;

/// <summary>
/// Provides role-based authorization services for checking user permissions.
/// </summary>
/// <remarks>
/// This service abstracts role-based authorization logic and can be implemented
/// to integrate with various authorization systems such as ASP.NET Core Identity,
/// custom role providers, or external authorization services.
/// </remarks>
public interface IRoleService
{
    /// <summary>
    /// Determines whether the user has at least one of the specified roles.
    /// </summary>
    /// <param name="user">The claims principal representing the user.</param>
    /// <param name="requiredRoles">The collection of roles to check against.</param>
    /// <returns>A task that represents the asynchronous operation. The task result is true if the user has at least one of the required roles; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="user"/> or <paramref name="requiredRoles"/> is null.</exception>
    Task<bool> UserHasAnyRoleAsync(ClaimsPrincipal user, IEnumerable<string> requiredRoles);
}