using Htmx.Components.Models;
using Htmx.Components.State;
using Htmx.Components.ViewResults;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Htmx.Components.Filters;

/// <summary>
/// A result filter that automatically injects updated page state into HTMX responses
/// when the page state has been modified during request processing.
/// This filter ensures that page state changes are persisted in the client browser
/// for subsequent HTMX requests.
/// </summary>
/// <remarks>
/// This filter only operates on <see cref="MultiSwapViewResult"/> instances and only
/// when the page state is marked as dirty (has been modified). When triggered, it
/// adds an out-of-band update that replaces the page state hidden input field
/// with the updated encrypted state data.
/// </remarks>
public class PageStateOobInjectorFilter : IAsyncResultFilter
{
    private readonly IPageState _pageState;

    /// <summary>
    /// Initializes a new instance of the PageStateOobInjectorFilter class.
    /// </summary>
    /// <param name="pageState">The page state service that tracks state modifications.</param>
    public PageStateOobInjectorFilter(IPageState pageState)
    {
        _pageState = pageState;
    }

    /// <summary>
    /// Executes the filter logic, injecting page state updates when necessary.
    /// The filter checks if the result is a MultiSwapViewResult and if the page state
    /// has been modified, then adds an out-of-band update for the page state container.
    /// </summary>
    /// <param name="context">The result executing context.</param>
    /// <param name="next">The next filter in the pipeline.</param>
    /// <returns>A task representing the asynchronous filter operation.</returns>
    public async Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
    {
        if (context.Result is MultiSwapViewResult multiSwap && _pageState.IsDirty)
        {
            var oobView = new HtmxViewInfo
            {
                ViewName = "_PageStateHiddenInput",
                Model = _pageState.Encrypted,
                TargetDisposition = OobTargetDisposition.OuterHtml
            };

            multiSwap.WithOobContent(oobView);
        }

        await next();
    }
}
