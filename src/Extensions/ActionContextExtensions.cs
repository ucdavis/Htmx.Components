using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Routing;

namespace Htmx.Components.Extensions;

/// <summary>
/// Provides extension methods for creating valid <see cref="ActionContext"/> instances from the current request.
/// </summary>
public static class ActionContextExtensions
{
    /// <summary>
    /// Provides a sanity check to fail early if the current request context is not initialized.
    /// </summary>
    /// <param name="httpContextAccessor">The HTTP context accessor to validate.</param>
    /// <returns>A valid <see cref="ActionContext"/> instance.</returns>
    /// <remarks>
    /// This method uses endpoint routing metadata to recover the current MVC
    /// <see cref="ActionDescriptor"/>.
    /// </remarks>
    /// <exception cref="InvalidOperationException">
    /// Thrown when any of the following conditions are true:
    /// <list type="bullet">
    /// <item><description>HttpContext is not available</description></item>
    /// <item><description>RouteData is not available</description></item>
    /// <item><description>ActionDescriptor is not available</description></item>
    /// </list>
    /// </exception>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="httpContextAccessor"/> is null.</exception>
    public static ActionContext GetValidActionContext(this IHttpContextAccessor httpContextAccessor)
    {
        if (httpContextAccessor == null) throw new ArgumentNullException(nameof(httpContextAccessor));

        var httpContext = httpContextAccessor.HttpContext;
        if (httpContext == null)
            throw new InvalidOperationException("HttpContext is not available.");

        var routeData = httpContext.GetRouteData();
        if (routeData == null)
            throw new InvalidOperationException("RouteData is not available.");

        var actionDescriptor = httpContext.GetEndpoint()?.Metadata.GetMetadata<ActionDescriptor>();
        if (actionDescriptor == null)
            throw new InvalidOperationException("ActionDescriptor is not available.");

        return new ActionContext(httpContext, routeData, actionDescriptor);
    }

#pragma warning disable ASPDEPR006
    /// <summary>
    /// Provides a sanity check to fail early if IActionContextAccessor is not initialized.
    /// </summary>
    /// <param name="actionContextAccessor">The action context accessor to validate.</param>
    /// <returns>A valid <see cref="ActionContext"/> instance.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when HttpContext, RouteData, or ActionDescriptor is not available.
    /// </exception>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="actionContextAccessor"/> is null.</exception>
    [Obsolete("IActionContextAccessor is obsolete in ASP.NET Core 10. Use IHttpContextAccessor with GetValidActionContext() instead.", DiagnosticId = "ASPDEPR006", UrlFormat = "https://aka.ms/aspnet/deprecate/006")]
    public static ActionContext GetValidActionContext(this IActionContextAccessor actionContextAccessor)
    {
        if (actionContextAccessor == null) throw new ArgumentNullException(nameof(actionContextAccessor));

        if (actionContextAccessor.ActionContext?.HttpContext == null)
            throw new InvalidOperationException("HttpContext is not available.");
        if (actionContextAccessor.ActionContext?.RouteData == null)
            throw new InvalidOperationException("RouteData is not available.");
        if (actionContextAccessor.ActionContext?.ActionDescriptor == null)
            throw new InvalidOperationException("ActionDescriptor is not available.");

        return actionContextAccessor.ActionContext;
    }
#pragma warning restore ASPDEPR006
}
