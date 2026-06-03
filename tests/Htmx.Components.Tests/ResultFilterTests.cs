using Htmx.Components.Filters;
using Htmx.Components.Models;
using Htmx.Components.Services;
using Htmx.Components.State;
using Htmx.Components.Table.Internal;
using Htmx.Components.Table.Models;
using Htmx.Components.ViewResults;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;

namespace Htmx.Components.Tests;

public class ResultFilterTests
{
    [Fact]
    public async Task PageStateOobInjectorFilter_AddsPageStateOobContentWhenDirty()
    {
        var pageState = new PageState(DataProtectionProvider.Create("filter-tests"));
        pageState.Set("table", "page", 2);
        var result = new MultiSwapViewResult().WithMainContent("_Main", new object());
        var context = CreateResultExecutingContext(result);
        var filter = new PageStateOobInjectorFilter(pageState);

        await filter.OnResultExecutionAsync(context, Next(context));
        await result.ExecuteResultAsync(context);

        var body = await MultiSwapViewResultTests.ReadBodyAsync(context.HttpContext.Response);
        Assert.Contains("id=\"page_state\"", body);
        Assert.Contains("hx-swap-oob=\"outerHTML\"", body);
    }

    [Fact]
    public async Task TableRefreshFilter_ConvertsHtmxObjectResultToMultiSwapResult()
    {
        await using var services = TestServices.CreateHtmxServices();
        var registry = new ModelRegistry(
            services,
            services.GetRequiredService<Authorization.IResourceOperationRegistry>());
        registry.Register<Widget, int>("Widget", (_, builder) => builder.WithKeySelector(widget => widget.Id));
        var handler = await registry.GetModelHandler<Widget, int>("Widget", ModelUI.Table);
        var tableModel = new TableModel<Widget, int>(new TableModelConfig<Widget, int>
        {
            ModelHandler = handler
        });
        var context = CreateResultExecutingContext(new OkObjectResult(tableModel));
        context.HttpContext.Request.Headers["HX-Request"] = "true";
        context.ActionDescriptor = new ControllerActionDescriptor
        {
            MethodInfo = typeof(FilterTestController).GetMethod(nameof(FilterTestController.Refresh))!
        };

        var filter = new TableOobRefreshFilter(new ViewPaths());

        await filter.OnResultExecutionAsync(context, Next(context));

        var multiSwap = Assert.IsType<MultiSwapViewResult>(context.Result);
        await multiSwap.ExecuteResultAsync(context);
        var body = await MultiSwapViewResultTests.ReadBodyAsync(context.HttpContext.Response);
        Assert.Contains("id=\"table-body\" hx-swap-oob=\"outerHTML\"", body);
        Assert.Contains("id=\"table-pagination\" hx-swap-oob=\"outerHTML\"", body);
    }

    private static ResultExecutingContext CreateResultExecutingContext(IActionResult result)
    {
        var actionContext = MultiSwapViewResultTests.CreateActionContext();
        return new ResultExecutingContext(
            actionContext,
            [],
            result,
            controller: new FilterTestController());
    }

    private static ResultExecutionDelegate Next(ResultExecutingContext context)
    {
        return () => Task.FromResult(new ResultExecutedContext(
            context,
            context.Filters,
            context.Result,
            context.Controller));
    }

    private sealed class FilterTestController : Controller
    {
        [TableRefreshAction]
        public IActionResult Refresh() => Ok();
    }
}
