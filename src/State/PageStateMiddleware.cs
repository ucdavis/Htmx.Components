using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace Htmx.Components.State;

public class PageStateMiddleware
{
    private readonly RequestDelegate _next;
    internal const string HttpContextPageStateKey = "PageState";
    internal const string PageStateHeaderKey = "X-Page-State";

    public PageStateMiddleware(RequestDelegate next)
    {
        _next = next;
    }

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

    public static IPageState GetPageState(HttpContext context)
    {
        return context.Items[HttpContextPageStateKey] as IPageState 
            ?? throw new InvalidOperationException("PageState not available");
    }
}
