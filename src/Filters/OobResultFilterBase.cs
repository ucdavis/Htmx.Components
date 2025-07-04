using Htmx.Components.NavBar;
using Htmx.Components.ViewResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace Htmx.Components.Filters;

/// <summary>
/// Base class for result filters that handle out-of-band (OOB) updates in HTMX responses.
/// This abstract class provides the common infrastructure for processing attributes that trigger
/// additional view renders to be included in HTMX responses for updating multiple page elements.
/// </summary>
/// <typeparam name="T">The attribute type that triggers the OOB behavior.</typeparam>
/// <remarks>
/// <para>
/// Derived classes only need to implement view name retrieval and multi-swap update logic.
/// The base class handles the detection of HTMX requests, attribute processing, and result transformation.
/// For non-HTMX requests, it can optionally render a full page view if implemented by the derived class.
/// </para>
/// <para><strong>Creating Custom OOB Filters:</strong></para>
/// <para>
/// This base class is designed to be extended for custom components that need coordinated updates.
/// Common scenarios include:
/// </para>
/// <list type="bullet">
/// <item>
/// <description>Authentication-related components that need to refresh together</description>
/// </item>
/// <item>
/// <description>Navigation and breadcrumb components that depend on current context</description>
/// </item>
/// <item>
/// <description>Dashboard widgets that need to reflect data changes</description>
/// </item>
/// <item>
/// <description>Shopping cart and inventory displays that must stay synchronized</description>
/// </item>
/// </list>
/// <para>
/// To create a custom filter, inherit from this class with your custom attribute type and implement
/// the abstract methods to define your specific OOB update behavior.
/// </para>
/// </remarks>
public abstract class OobResultFilterBase<T> : IAsyncResultFilter
    where T : Attribute
{
    /// <summary>
    /// Executes the result filter logic, processing actions marked with the target attribute type.
    /// For HTMX requests, converts the result to a MultiSwapViewResult with OOB updates.
    /// For non-HTMX requests, optionally renders a full page view if implemented by the derived class.
    /// </summary>
    /// <param name="context">The result executing context.</param>
    /// <param name="next">The next filter in the pipeline.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
    {
        if (context.ActionDescriptor is ControllerActionDescriptor cad)
        {
            var attribute = cad.MethodInfo.GetCustomAttributes(typeof(T), true).Cast<T>().FirstOrDefault();
            if (attribute != null && (
                context.Result is ObjectResult
                || context.Result is MultiSwapViewResult
                || context.Result is OkResult))
            {
                if (context.HttpContext.Request.IsHtmx())
                {
                    MultiSwapViewResult multiSwapViewResult = null!;
                    multiSwapViewResult = context.Result switch
                    {
                        ObjectResult objResult => new MultiSwapViewResult
                        {
                            Model = objResult.Value
                        },
                        // TODO: OkResult is a special case that isn't applicable to all subclasses. Find a way to handle this more gracefully.
                        OkResult => new MultiSwapViewResult(),
                        MultiSwapViewResult msvr => msvr,
                        _ => throw new InvalidOperationException("Unsupported result type.")
                    };
                    await UpdateMultiSwapViewResultAsync(attribute, multiSwapViewResult, context);
                    context.Result = multiSwapViewResult;
                }
                else
                {
                    // TODO: OkResult is a special case that isn't applicable to all subclasses. Find a way to handle this more gracefully.
                    if (context.Result is OkResult)
                    {
                        await next();
                        return;
                    }
                    var viewName = await GetViewNameForNonHtmxRequest(attribute, cad);
                    var controller = (Controller)context.Controller;
                    context.Result = new ViewResult
                    {
                        ViewName = viewName,
                        ViewData = new ViewDataDictionary(controller.ViewData)
                        {
                            Model = context.Result is ObjectResult obj ? obj.Value : null
                        },
                        TempData = controller.TempData
                    };
                }
            }
        }
        await next();
    }

    /// <summary>
    /// Gets the view name to render for non-HTMX requests when the action has the target attribute.
    /// The default implementation returns null, which means no special handling for non-HTMX requests.
    /// </summary>
    /// <param name="attribute">The attribute instance found on the action method.</param>
    /// <param name="cad">The controller action descriptor for the current action.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the view name, or null if no special view should be rendered.</returns>
    /// <remarks>
    /// Derived classes should override this method if they need to provide a specific view for non-HTMX requests.
    /// If the filter should not handle non-HTMX requests at all, this method can be overridden to throw an exception.
    /// </remarks>
    protected virtual Task<string?> GetViewNameForNonHtmxRequest(T attribute, ControllerActionDescriptor cad)
    {
        // Default implementation returns null, derived classes should override this to provide a view name.
        return Task.FromResult<string?>(null);
    }
    
    /// <summary>
    /// Updates the MultiSwapViewResult with additional view renders based on the attribute configuration.
    /// This method is called for HTMX requests and should add any necessary out-of-band updates.
    /// </summary>
    /// <param name="attribute">The attribute instance found on the action method.</param>
    /// <param name="multiSwapViewResult">The MultiSwapViewResult to update with additional renders.</param>
    /// <param name="context">The result executing context for accessing request and controller information.</param>
    /// <returns>A task representing the asynchronous update operation.</returns>
    protected abstract Task UpdateMultiSwapViewResultAsync(T attribute, MultiSwapViewResult multiSwapViewResult, ResultExecutingContext context);

}

//TODO: If the number of subclasses or special edge cases balloons, consider a more generic pluggable system
// public interface IMultiSwapViewResultMutator
// {
//     Task MutateAsync(ResultExecutingContext context, MultiSwapViewResult result);
// }
//
// public class HtmxPipelineResultFilter : IAsyncResultFilter
// {
//     private readonly IEnumerable<IMultiSwapViewResultMutator> _mutators;
//     public HtmxPipelineResultFilter(IEnumerable<IMultiSwapViewResultMutator> mutators)
//         => _mutators = mutators;
//     public async Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
//     {
//         await next();
//         if (context.Result is MultiSwapViewResult result)
//         {
//             foreach (var mutator in _mutators)
//                 await mutator.MutateAsync(context, result);
//         }
//     }
// }