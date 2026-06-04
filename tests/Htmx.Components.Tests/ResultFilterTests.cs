using Htmx.Components.Filters;
using Htmx.Components.Models;
using Htmx.Components.Services;
using Htmx.Components.State;
using Htmx.Components.Table;
using Htmx.Components.Table.Internal;
using Htmx.Components.Table.Models;
using Htmx.Components.ViewResults;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
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
            ModelHandler = handler,
            ComponentId = "hc-table-alpha"
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
        Assert.Contains("id=\"hc-table-alpha-body\" hx-swap-oob=\"outerHTML\"", body);
        Assert.Contains("id=\"hc-table-alpha-pagination\" hx-swap-oob=\"outerHTML\"", body);
    }

    [Fact]
    public async Task TableComponentIdentity_ScopesStateDomTargetsAndActionValues()
    {
        await using var services = TestServices.CreateHtmxServices();
        var registry = new ModelRegistry(
            services,
            services.GetRequiredService<Authorization.IResourceOperationRegistry>());
        registry.Register<Widget, int>("Widget", (_, builder) => builder.WithKeySelector(widget => widget.Id));
        var handler = await registry.GetModelHandler<Widget, int>("Widget", ModelUI.Table);
        var first = TableComponentIdentity.Ensure("admin");
        var second = TableComponentIdentity.Ensure("reports");

        var firstTable = new TableModel<Widget, int>(new TableModelConfig<Widget, int>
        {
            ModelHandler = handler,
            ComponentId = first
        });
        var secondTable = new TableModel<Widget, int>(new TableModelConfig<Widget, int>
        {
            ModelHandler = handler,
            ComponentId = second
        });
        var row = new TableRowContext<Widget, int>
        {
            Item = new Widget { Id = 1, Name = "Alpha" },
            ModelHandler = handler,
            Key = 1
        };

        Assert.NotEqual(TableComponentIdentity.TableStatePartition(first), TableComponentIdentity.TableStatePartition(second));
        Assert.NotEqual(TableComponentIdentity.BodyId(firstTable), TableComponentIdentity.BodyId(secondTable));
        Assert.NotEqual(TableComponentIdentity.RowId(firstTable, row), TableComponentIdentity.RowId(secondTable, row));
        Assert.Contains(first, TableComponentIdentity.HxVals(first));
    }

    [Fact]
    public async Task OobResultFilterBase_ConvertsNonHtmxObjectResultToConfiguredView()
    {
        var context = CreateResultExecutingContext(new OkObjectResult(new Widget { Id = 8, Name = "Beta" }));
        context.ActionDescriptor = new ControllerActionDescriptor
        {
            MethodInfo = typeof(FilterTestController).GetMethod(nameof(FilterTestController.PipelineObject))!
        };
        var filter = new PipelineTestFilter();

        await filter.OnResultExecutionAsync(context, Next(context));

        var view = Assert.IsType<ViewResult>(context.Result);
        Assert.Equal("_PipelineFull", view.ViewName);
        var model = Assert.IsType<Widget>(view.ViewData.Model);
        Assert.Equal(8, model.Id);
    }

    [Fact]
    public async Task OobResultFilterBase_LeavesNonHtmxOkResultUnchanged()
    {
        var ok = new OkResult();
        var context = CreateResultExecutingContext(ok);
        context.ActionDescriptor = new ControllerActionDescriptor
        {
            MethodInfo = typeof(FilterTestController).GetMethod(nameof(FilterTestController.PipelineOk))!
        };
        var filter = new PipelineTestFilter();

        await filter.OnResultExecutionAsync(context, Next(context));

        Assert.Same(ok, context.Result);
        Assert.False(filter.WasMutated);
    }

    private static ResultExecutingContext CreateResultExecutingContext(IActionResult result)
    {
        var actionContext = MultiSwapViewResultTests.CreateActionContext();
        actionContext.ActionDescriptor = new ControllerActionDescriptor
        {
            MethodInfo = typeof(FilterTestController).GetMethod(nameof(FilterTestController.Refresh))!
        };
        var controller = new FilterTestController
        {
            ControllerContext = new ControllerContext(actionContext),
            TempData = new TempDataDictionary(actionContext.HttpContext, new NullTempDataProvider())
        };

        return new ResultExecutingContext(
            actionContext,
            [],
            result,
            controller);
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

        [PipelineTest]
        public IActionResult PipelineObject() => Ok();

        [PipelineTest]
        public IActionResult PipelineOk() => Ok();
    }

    private sealed class PipelineTestAttribute : Attribute;

    private sealed class PipelineTestFilter : OobResultFilterBase<PipelineTestAttribute>
    {
        public bool WasMutated { get; private set; }

        protected override Task<string?> GetViewNameForNonHtmxRequest(
            PipelineTestAttribute attribute,
            ControllerActionDescriptor cad)
        {
            return Task.FromResult<string?>("_PipelineFull");
        }

        protected override Task UpdateMultiSwapViewResultAsync(
            PipelineTestAttribute attribute,
            MultiSwapViewResult multiSwapViewResult,
            ResultExecutingContext context)
        {
            WasMutated = true;
            return Task.CompletedTask;
        }
    }

    private sealed class NullTempDataProvider : ITempDataProvider
    {
        public IDictionary<string, object> LoadTempData(HttpContext context) => new Dictionary<string, object>();
        public void SaveTempData(HttpContext context, IDictionary<string, object> values) { }
    }

}
