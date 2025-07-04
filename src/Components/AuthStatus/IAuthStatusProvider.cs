using System.Security.Claims;
using Htmx.Components.AuthStatus.Models;

namespace Htmx.Components.AuthStatus;

/// <summary>
/// Provides authentication status information for display in authentication status components.
/// </summary>
/// <remarks>
/// Implementations of this interface are responsible for extracting user information
/// from the security context and formatting it for display. This allows applications
/// to customize how authentication status is presented based on their specific
/// authentication systems and user models.
/// </remarks>
/// <example>
/// <code>
/// public class CustomAuthStatusProvider : IAuthStatusProvider
/// {
///     public async Task&lt;AuthStatusViewModel&gt; GetAuthStatusAsync(ClaimsPrincipal user)
///     {
///         var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
///         var userProfile = await _userService.GetProfileAsync(userId);
///         
///         return new AuthStatusViewModel
///         {
///             IsAuthenticated = user.Identity?.IsAuthenticated ?? false,
///             UserName = userProfile?.DisplayName,
///             ProfileImageUrl = userProfile?.AvatarUrl,
///             LoginUrl = "/Account/Login"
///         };
///     }
/// }
/// </code>
/// </example>
public interface IAuthStatusProvider
{
    /// <summary>
    /// Gets authentication status information for the specified user.
    /// </summary>
    /// <param name="user">The user principal to get status information for.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the authentication status view model.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="user"/> is null.</exception>
    Task<AuthStatusViewModel> GetAuthStatusAsync(ClaimsPrincipal user);
}