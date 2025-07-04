namespace Htmx.Components.AuthStatus.Models;

/// <summary>
/// View model that contains authentication status information for display in views.
/// </summary>
/// <remarks>
/// This model is used by authentication status views to display user information
/// and authentication-related UI elements such as login links and user avatars.
/// </remarks>
public class AuthStatusViewModel
{
    /// <summary>
    /// Gets or sets a value indicating whether the current user is authenticated.
    /// </summary>
    /// <value>true if the user is authenticated; otherwise, false.</value>
    public bool IsAuthenticated { get; set; }

    /// <summary>
    /// Gets or sets the display name of the authenticated user.
    /// </summary>
    /// <value>The user's display name, or null if not authenticated or if no name is available.</value>
    public string? UserName { get; set; }

    /// <summary>
    /// Gets or sets the URL of the user's profile image.
    /// </summary>
    /// <value>A URL pointing to the user's profile image, or null if no image is available.</value>
    public string? ProfileImageUrl { get; set; }

    /// <summary>
    /// Gets or sets the URL to redirect users to for authentication.
    /// </summary>
    /// <value>The login URL, or null if login is not applicable.</value>
    public string? LoginUrl { get; set; }
}