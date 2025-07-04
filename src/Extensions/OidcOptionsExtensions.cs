using Microsoft.AspNetCore.Authentication.OpenIdConnect;

namespace Htmx.Components;

/// <summary>
/// Provides extension methods for configuring OpenID Connect authentication with HTMX support.
/// </summary>
/// <remarks>
/// These extensions modify the OIDC authentication flow to work seamlessly with HTMX requests
/// by providing alternative handling for authentication challenges and access denied scenarios.
/// Instead of performing redirects that would break HTMX functionality, these methods return
/// appropriate HTTP status codes and headers that HTMX can handle.
/// </remarks>
public static class OidcOptionsExtensions
{
    /// <summary>
    /// Configures OpenID Connect options to handle authentication popups for HTMX requests.
    /// </summary>
    /// <param name="oidc">The OpenID Connect options to configure.</param>
    /// <param name="url">The URL to display in the authentication popup.</param>
    /// <remarks>
    /// <para>
    /// This method modifies the OIDC authentication flow to be compatible with HTMX requests.
    /// When an HTMX request encounters an authentication challenge, instead of performing
    /// a redirect that would break the HTMX flow, it returns a 401 status with a custom
    /// header that instructs the client to open a popup for authentication.
    /// </para>
    /// <para>
    /// Similarly, when access is denied, it returns a 403 status with an HTMX trigger
    /// that can be handled by client-side JavaScript.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// services.Configure&lt;OpenIdConnectOptions&gt;("oidc", options =>
    /// {
    ///     options.ConfigureHtmxAuthPopup("/auth/login");
    ///     // ... other OIDC configuration
    /// });
    /// </code>
    /// </example>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="oidc"/> or <paramref name="url"/> is null.</exception>
    public static void ConfigureHtmxAuthPopup(this OpenIdConnectOptions oidc, string url)
    {
        if (oidc == null) throw new ArgumentNullException(nameof(oidc));
        if (url == null) throw new ArgumentNullException(nameof(url));
        
        oidc.Events.OnRedirectToIdentityProvider = context =>
        {
            if (context.Request.IsHtmx())
            {
                // We need to tell HTMX to redirect to the login page and skip the default redirect
                context.Response.StatusCode = 401;
                context.Response.Headers["X-Auth-Failure"] = $"popup-login:{url}";
                context.HandleResponse();
            }
            return Task.CompletedTask;
        };
        oidc.Events.OnAccessDenied = context =>
        {
            if (context.Request.IsHtmx())
            {
                // We need to tell HTMX that auth was denied and skip the default redirect
                context.Response.StatusCode = 403;
                context.Response.Headers["HX-Trigger"] = "auth-denied";
                context.HandleResponse();
            }
            return Task.CompletedTask;
        };
    }
}