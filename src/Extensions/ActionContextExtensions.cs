using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;

namespace Htmx.Components.Extensions;

/// <summary>
/// Provides extension methods for <see cref="IActionContextAccessor"/> to ensure valid action contexts.
/// </summary>
public static class ActionContextExtensions
{
    /// <summary>
    /// Provides a sanity check to fail early if IActionContextAccessor is not initialized
    /// </summary>
    /// <param name="actionContextAccessor">The action context accessor to validate.</param>
    /// <returns>A valid <see cref="ActionContext"/> instance.</returns>
    /// <remarks>
    /// This method performs comprehensive validation to ensure that all required components
    /// of the action context are available. It should be called before accessing any
    /// action context properties to prevent null reference exceptions.
    /// </remarks>
    /// <exception cref="InvalidOperationException">
    /// Thrown when any of the following conditions are true:
    /// <list type="bullet">
    /// <item><description>HttpContext is not available</description></item>
    /// <item><description>RouteData is not available</description></item>
    /// <item><description>ActionDescriptor is not available</description></item>
    /// </list>
    /// </exception>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="actionContextAccessor"/> is null.</exception>
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
}