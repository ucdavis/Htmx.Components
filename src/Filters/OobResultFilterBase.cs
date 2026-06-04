using Htmx.Components.NavBar;
using Htmx.Components.ViewResults;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;

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
    private readonly HtmxResponsePipeline<T> _pipeline = new();

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
        await _pipeline.ExecuteAsync(context, next, new OobResultMutator(this));
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

    private sealed class OobResultMutator : IHtmxResponseMutator<T>
    {
        private readonly OobResultFilterBase<T> _filter;

        public OobResultMutator(OobResultFilterBase<T> filter)
        {
            _filter = filter;
        }

        public Task MutateAsync(HtmxResponseMutation<T> mutation)
        {
            return _filter.UpdateMultiSwapViewResultAsync(
                mutation.Attribute,
                mutation.Result,
                mutation.ResultContext);
        }

        public Task<string?> GetViewNameForNonHtmxRequestAsync(T attribute, ControllerActionDescriptor actionDescriptor)
        {
            return _filter.GetViewNameForNonHtmxRequest(attribute, actionDescriptor);
        }
    }
}
