using Htmx.Components.State;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Htmx.Components;

/// <summary>
/// Provides extension methods for accessing page state from HTTP contexts and controllers.
/// </summary>
/// <remarks>
/// These extensions provide convenient access to the page state service that is attached
/// to the HTTP context by the <see cref="PageStateMiddleware"/>. Page state allows
/// maintaining data across HTMX requests without relying on session state or hidden form fields.
/// This is particularly useful for HTMX applications where partial page updates need to preserve
/// component state, form data, or UI context. Unlike session state which persists across browser
/// sessions, page state is scoped to a single page lifecycle and automatically cleaned up when
/// the user navigates away. This provides better memory usage and avoids stale data issues
/// while still enabling rich interactive experiences with HTMX partial updates.
/// </remarks>
public static class PageStateExtensions
{
    /// <summary>
    /// Gets the page state service from the HTTP context.
    /// </summary>
    /// <param name="context">The HTTP context to retrieve page state from.</param>
    /// <returns>The page state service instance attached to the context.</returns>
    /// <remarks>
    /// This method requires that <see cref="PageStateMiddleware"/> has been added to the
    /// application pipeline and has processed the current request. The page state is
    /// automatically attached to the HTTP context items during middleware execution.
    /// </remarks>
    /// <exception cref="InvalidOperationException">
    /// Thrown when page state is not found in the context, typically indicating that
    /// <see cref="PageStateMiddleware"/> is not registered or not properly configured.
    /// </exception>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="context"/> is null.</exception>
    public static IPageState GetPageState(this HttpContext context)
    {
        if (context == null) throw new ArgumentNullException(nameof(context));
        
        if (context.Items.TryGetValue(PageStateMiddleware.HttpContextPageStateKey, out var value) && value is IPageState pageState)
            return pageState;
        throw new InvalidOperationException("PageState not found. Is PageStateMiddleware registered?");
    }

    /// <summary>
    /// Gets the page state service from a controller's HTTP context.
    /// </summary>
    /// <param name="controller">The controller to retrieve page state from.</param>
    /// <returns>The page state service instance attached to the controller's HTTP context.</returns>
    /// <remarks>
    /// This is a convenience method that extracts the HTTP context from the controller
    /// and retrieves the page state service. It provides the same functionality as
    /// calling <see cref="GetPageState(HttpContext)"/> on the controller's HTTP context.
    /// </remarks>
    /// <exception cref="InvalidOperationException">
    /// Thrown when page state is not found in the context, typically indicating that
    /// <see cref="PageStateMiddleware"/> is not registered or not properly configured.
    /// </exception>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="controller"/> is null.</exception>
    public static IPageState GetPageState(this Controller controller)
    {
        if (controller == null) throw new ArgumentNullException(nameof(controller));
        
        return controller.HttpContext.GetPageState();
    }
}
