using System.Text.Encodings.Web;
using Htmx.Components.Models;
using Htmx.Components.ViewResults;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.DependencyInjection;

namespace Htmx.Components.Tests;

public class MultiSwapViewResultTests
{
    [Fact]
    public async Task ExecuteResultAsync_WrapsOutOfBandContentWithDispositionAndTarget()
    {
        var context = CreateActionContext();
        var result = new MultiSwapViewResult()
            .WithMainContent("_Main", new object())
            .WithOobContent("_Panel", new Targetable("#target-panel", OobTargetDisposition.InnerHtml));

        await result.ExecuteResultAsync(context);

        var body = await ReadBodyAsync(context.HttpContext.Response);
        Assert.Contains("""<section id="content">_Main</section>""", body);
        Assert.Contains("""<template><section id="content" hx-swap-oob="innerHTML:#target-panel">_Panel</section></template>""", body);
    }

    [Fact]
    public async Task ExecuteResultAsync_AllowsCssTargetSelectors()
    {
        var context = CreateActionContext();
        var targetSelector = """tbody > tr[data-id="x"]:nth-child(2)""";
        var targetable = new Targetable(targetSelector, OobTargetDisposition.OuterHtml);
        var result = new MultiSwapViewResult()
            .WithOobContent("_Panel", targetable);

        await result.ExecuteResultAsync(context);

        var body = await ReadBodyAsync(context.HttpContext.Response);
        Assert.Equal(targetSelector, targetable.TargetSelector);
        var expectedAttribute = $"hx-swap-oob=\"outerHTML:{HtmlEncoder.Default.Encode(targetSelector)}\"";
        Assert.Contains("&quot;", expectedAttribute);
        Assert.Contains(expectedAttribute, body);
    }

    [Fact]
    public void AddHxSwapToOuterElement_SkipsLeadingCommentsAndHandlesQuotedAttributes()
    {
        var wrapped = MultiSwapViewResult.AddHxSwapToOuterElement(
            """<!-- before --><section data-expression="a > b">content</section>""",
            new HtmxViewInfo
            {
                TargetDisposition = OobTargetDisposition.BeforeEnd,
                TargetSelector = "#messages"
            });

        Assert.Equal(
            """<template><!-- before --><section data-expression="a > b" hx-swap-oob="beforeend:#messages">content</section></template>""",
            wrapped);
    }

    [Fact]
    public void AddHxSwapToOuterElement_DoesNotRewriteExistingOobAttribute()
    {
        var html = """<section hx-swap-oob="outerHTML:#existing">content</section>""";

        var wrapped = MultiSwapViewResult.AddHxSwapToOuterElement(
            html,
            new HtmxViewInfo
            {
                TargetDisposition = OobTargetDisposition.InnerHtml,
                TargetSelector = "#other"
            });

        Assert.Equal(html, wrapped);
    }

    internal static ActionContext CreateActionContext()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<ICompositeViewEngine, StubCompositeViewEngine>();
        services.AddSingleton<ITempDataProvider, TempDataDictionaryFactory>();
        var httpContext = new DefaultHttpContext
        {
            RequestServices = services.BuildServiceProvider()
        };
        httpContext.Response.Body = new MemoryStream();

        return new ActionContext(httpContext, new Microsoft.AspNetCore.Routing.RouteData(), new());
    }

    internal static async Task<string> ReadBodyAsync(HttpResponse response)
    {
        response.Body.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(response.Body, leaveOpen: true);
        return await reader.ReadToEndAsync();
    }

    private sealed record Targetable(string? TargetSelector, OobTargetDisposition? TargetDisposition) : IOobTargetable;

    private sealed class TempDataDictionaryFactory : ITempDataProvider
    {
        public IDictionary<string, object> LoadTempData(HttpContext context) => new Dictionary<string, object>();
        public void SaveTempData(HttpContext context, IDictionary<string, object> values) { }
    }
}
