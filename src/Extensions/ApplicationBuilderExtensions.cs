using Htmx.Components.State;
using Microsoft.AspNetCore.Builder;

namespace Htmx.Components;

/// <summary>
/// Extension methods for configuring HTMX Components middleware in the application pipeline.
/// </summary>
public static class ApplicationBuilderExtensions
{
    /// <summary>
    /// Adds the HTMX page state middleware to the application pipeline.
    /// </summary>
    /// <param name="app">The application builder.</param>
    /// <returns>The application builder for method chaining.</returns>
    /// <remarks>
    /// This middleware must be added to the pipeline to enable page state functionality.
    /// It should be added early in the pipeline, typically before authentication and authorization middleware.
    /// </remarks>
    /// <example>
    /// <code>
    /// app.UseHtmxPageState();
    /// app.UseAuthentication();
    /// app.UseAuthorization();
    /// app.MapControllers();
    /// </code>
    /// </example>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="app"/> is null.</exception>
    public static IApplicationBuilder UseHtmxPageState(this IApplicationBuilder app)
    {
        if (app == null) throw new ArgumentNullException(nameof(app));
        
        app.UseMiddleware<PageStateMiddleware>();
        return app;
    }
}