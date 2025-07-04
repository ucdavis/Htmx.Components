using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace Htmx.Components.State;

/// <summary>
/// ASP.NET Core middleware that manages page state across HTTP requests.
/// This middleware automatically loads encrypted page state from request headers,
/// makes it available throughout the request pipeline, and manages state persistence.
/// </summary>
public class PageStateMiddleware
{
    private readonly RequestDelegate _next;
    internal const string HttpContextPageStateKey = "PageState";
    internal const string PageStateHeaderKey = "X-Page-State";

    /// <summary>
    /// Initializes a new instance of the PageStateMiddleware with the specified next delegate.
    /// </summary>
    /// <param name="next">The next middleware delegate in the pipeline</param>
    public PageStateMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    /// <summary>
    /// Processes the HTTP request, loading page state from headers and making it available to the request context.
    /// The page state is loaded from the X-Page-State header if present, and attached to the HttpContext for use by subsequent middleware and controllers.
    /// </summary>
    /// <param name="context">The HTTP context for the current request</param>
    /// <param name="pageState">The page state service injected by dependency injection</param>
    /// <returns>A task representing the asynchronous middleware operation</returns>
    public async Task InvokeAsync(HttpContext context, IPageState pageState)
    {
        string? encryptedState = null;

        if (context.Request.Headers.TryGetValue(PageStateHeaderKey, out var headerValue))
        {
            encryptedState = headerValue.FirstOrDefault();
        }

        if (!string.IsNullOrEmpty(encryptedState))
        {
            pageState.Load(encryptedState);
        }

        // Attach to HttpContext
        context.Items[HttpContextPageStateKey] = pageState;

        await _next(context);
    }

    /// <summary>
    /// Retrieves the page state from the current HTTP context.
    /// This method provides access to the page state that was loaded and attached by the middleware.
    /// </summary>
    /// <param name="context">The HTTP context from which to retrieve the page state</param>
    /// <returns>The page state instance for the current request</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when page state is not available, typically indicating that the PageStateMiddleware was not properly registered or executed.
    /// </exception>
    public static IPageState GetPageState(HttpContext context)
    {
        return context.Items[HttpContextPageStateKey] as IPageState 
            ?? throw new InvalidOperationException("PageState not available");
    }
}
