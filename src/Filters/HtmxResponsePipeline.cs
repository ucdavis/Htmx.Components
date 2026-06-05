using Htmx.Components.ViewResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace Htmx.Components.Filters;

internal interface IHtmxResponseMutator<T>
    where T : Attribute
{
    Task MutateAsync(HtmxResponseMutation<T> mutation);
    Task<string?> GetViewNameForNonHtmxRequestAsync(T attribute, ControllerActionDescriptor actionDescriptor);
}

internal sealed record HtmxResponseMutation<T>(
    T Attribute,
    MultiSwapViewResult Result,
    ResultExecutingContext ResultContext,
    ControllerActionDescriptor ActionDescriptor)
    where T : Attribute;

internal sealed class HtmxResponsePipeline<T>
    where T : Attribute
{
    public async Task ExecuteAsync(
        ResultExecutingContext context,
        ResultExecutionDelegate next,
        IHtmxResponseMutator<T> mutator)
    {
        if (!TryGetAttribute(context, out var actionDescriptor, out var attribute) ||
            !CanProcess(context.Result) ||
            IsHandledErrorObjectResult(context.Result))
        {
            await next();
            return;
        }

        if (context.HttpContext.Request.IsHtmx())
        {
            var multiSwapResult = ToMultiSwapResult(context.Result);
            await mutator.MutateAsync(new HtmxResponseMutation<T>(
                attribute,
                multiSwapResult,
                context,
                actionDescriptor));
            context.Result = multiSwapResult;
        }
        else if (TryCreateFullPageViewResult(context, actionDescriptor, attribute, mutator, out var viewResultTask))
        {
            var viewResult = await viewResultTask;
            if (viewResult is not null)
            {
                context.Result = viewResult;
            }
        }

        await next();
    }

    private static bool TryGetAttribute(
        ResultExecutingContext context,
        out ControllerActionDescriptor actionDescriptor,
        out T attribute)
    {
        if (context.ActionDescriptor is ControllerActionDescriptor candidate)
        {
            var found = candidate.MethodInfo.GetCustomAttributes(typeof(T), true).Cast<T>().FirstOrDefault();
            if (found is not null)
            {
                actionDescriptor = candidate;
                attribute = found;
                return true;
            }
        }

        actionDescriptor = null!;
        attribute = null!;
        return false;
    }

    private static bool CanProcess(IActionResult result)
    {
        return result is ObjectResult or MultiSwapViewResult or OkResult;
    }

    private static bool IsHandledErrorObjectResult(IActionResult result)
    {
        return result is ObjectResult { StatusCode: >= 400 };
    }

    private static MultiSwapViewResult ToMultiSwapResult(IActionResult result)
    {
        return result switch
        {
            ObjectResult objectResult => new MultiSwapViewResult
            {
                Model = objectResult.Value
            },
            MultiSwapViewResult multiSwapResult => multiSwapResult,
            OkResult => new MultiSwapViewResult(),
            _ => throw new InvalidOperationException($"Result type '{result.GetType().FullName}' cannot be converted to {nameof(MultiSwapViewResult)}.")
        };
    }

    private static bool TryCreateFullPageViewResult(
        ResultExecutingContext context,
        ControllerActionDescriptor actionDescriptor,
        T attribute,
        IHtmxResponseMutator<T> mutator,
        out Task<ViewResult?> viewResultTask)
    {
        var (hasModel, model) = context.Result switch
        {
            ObjectResult objectResult => (true, objectResult.Value),
            MultiSwapViewResult { Model: not null } multiSwapResult => (true, multiSwapResult.Model),
            _ => (false, null)
        };

        if (!hasModel)
        {
            viewResultTask = null!;
            return false;
        }

        viewResultTask = CreateFullPageViewResultAsync(context, actionDescriptor, attribute, mutator, model);
        return true;
    }

    private static async Task<ViewResult?> CreateFullPageViewResultAsync(
        ResultExecutingContext context,
        ControllerActionDescriptor actionDescriptor,
        T attribute,
        IHtmxResponseMutator<T> mutator,
        object? model)
    {
        var viewName = await mutator.GetViewNameForNonHtmxRequestAsync(attribute, actionDescriptor);
        if (viewName is null)
        {
            return null;
        }

        var controller = (Controller)context.Controller;
        return new ViewResult
        {
            ViewName = viewName,
            ViewData = new ViewDataDictionary(controller.ViewData)
            {
                Model = model
            },
            TempData = controller.TempData
        };
    }
}
