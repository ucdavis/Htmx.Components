using Htmx.Components.ViewResults;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;

namespace Htmx.Components.Filters;

/// <summary>
/// Converts unhandled HTMX action exceptions into safe error fragments while preserving detailed server logs.
/// </summary>
internal sealed class HtmxExceptionFilter : IAsyncExceptionFilter
{
    private readonly ILogger<HtmxExceptionFilter> _logger;

    public HtmxExceptionFilter(ILogger<HtmxExceptionFilter> logger)
    {
        _logger = logger;
    }

    public Task OnExceptionAsync(ExceptionContext context)
    {
        if (!context.HttpContext.Request.IsHtmx())
        {
            return Task.CompletedTask;
        }

        _logger.LogError(context.Exception, "Unhandled exception while processing an HTMX request.");
        context.Result = HtmxErrorResult.InternalServerError();
        context.ExceptionHandled = true;
        return Task.CompletedTask;
    }
}
