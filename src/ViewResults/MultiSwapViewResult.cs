using System.Text.Encodings.Web;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Htmx.Components.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewComponents;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.DependencyInjection;

namespace Htmx.Components.ViewResults;

/// <summary>
/// Returns multple htmx views within a single response
/// </summary>
public class MultiSwapViewResult : IActionResult
{
    private HtmxViewInfo? _main;
    private readonly List<HtmxViewInfo> _oobs = new();

    // holds original model in case it's needed for further processing such as in result filters
    /// <summary>
    /// Gets or sets the original model that may be needed for further processing such as in result filters.
    /// </summary>
    [JsonIgnore]
    public object? Model { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="MultiSwapViewResult"/> class.
    /// </summary>
    public MultiSwapViewResult()
    { }

    /// <summary>
    /// Initializes a new instance of the <see cref="MultiSwapViewResult"/> class with optional main content and out-of-band views.
    /// </summary>
    /// <param name="main">A tuple containing the partial view name and model for the main content, or null if no main content.</param>
    /// <param name="oobs">An array of out-of-band view information objects.</param>
    protected MultiSwapViewResult(
        (string PartialView, object Model)? main = null,
        params HtmxViewInfo[] oobs)
    {
        _main = main is not null
            ? new HtmxViewInfo
            {
                ViewName = main.Value.PartialView,
                Model = main.Value.Model,
                TargetDisposition = OobTargetDisposition.None
            }
            : null;
        _oobs.AddRange(oobs);
    }

    /// <summary>
    /// Sets the main content for the response using the specified view name and model.
    /// </summary>
    /// <param name="viewName">The name of the view to render as main content.</param>
    /// <param name="model">The model to pass to the view.</param>
    /// <returns>The current <see cref="MultiSwapViewResult"/> instance for method chaining.</returns>
    public MultiSwapViewResult WithMainContent(string viewName, object model)
    {
        _main = new HtmxViewInfo
        {
            ViewName = viewName,
            Model = model,
            TargetDisposition = OobTargetDisposition.None
        };
        return this;
    }

    /// <summary>
    /// Adds out-of-band content to the response with specified targeting options.
    /// </summary>
    /// <param name="viewName">The name of the view to render as out-of-band content.</param>
    /// <param name="model">The model to pass to the view.</param>
    /// <param name="targetDisposition">The disposition that determines how the content should be swapped (default is OuterHtml).</param>
    /// <param name="targetSelector">An optional CSS selector to target a specific element for the swap.</param>
    /// <returns>The current <see cref="MultiSwapViewResult"/> instance for method chaining.</returns>
    public MultiSwapViewResult WithOobContent(string viewName, object model, 
        OobTargetDisposition targetDisposition = OobTargetDisposition.OuterHtml, string? targetSelector = null)
    {
        _oobs.Add(new HtmxViewInfo
        {
            ViewName = viewName,
            Model = model,
            TargetDisposition = targetDisposition,
            TargetSelector = targetSelector
        });
        return this;
    }

    /// <summary>
    /// Adds out-of-band content to the response, automatically determining targeting options from the model if it implements <see cref="IOobTargetable"/>.
    /// </summary>
    /// <param name="viewName">The name of the view to render as out-of-band content.</param>
    /// <param name="model">The model to pass to the view. If the model implements <see cref="IOobTargetable"/>, its targeting properties will be used.</param>
    /// <returns>The current <see cref="MultiSwapViewResult"/> instance for method chaining.</returns>
    public MultiSwapViewResult WithOobContent(string viewName, object model)
    {
        _oobs.Add(new HtmxViewInfo
        {
            ViewName = viewName,
            Model = model,
            TargetDisposition = model is IOobTargetable t1
                ? t1.TargetDisposition ?? OobTargetDisposition.OuterHtml
                : OobTargetDisposition.OuterHtml,
            TargetSelector = model is IOobTargetable t2
                ? t2.TargetSelector
                : null
        });
        return this;
    }


    /// <summary>
    /// Adds multiple out-of-band content items to the response.
    /// </summary>
    /// <param name="oobList">A collection of <see cref="HtmxViewInfo"/> objects representing the out-of-band content to add.</param>
    /// <returns>The current <see cref="MultiSwapViewResult"/> instance for method chaining.</returns>
    public MultiSwapViewResult WithOobContent(IEnumerable<HtmxViewInfo> oobList)
    {
        _oobs.AddRange(oobList);
        return this;
    }

    /// <summary>
    /// Adds a single out-of-band content item to the response.
    /// </summary>
    /// <param name="oob">The <see cref="HtmxViewInfo"/> object representing the out-of-band content to add.</param>
    /// <returns>The current <see cref="MultiSwapViewResult"/> instance for method chaining.</returns>
    public MultiSwapViewResult WithOobContent(HtmxViewInfo oob)
    {
        _oobs.Add(oob);
        return this;
    }

    /// <summary>
    /// Executes the result operation, rendering all main and out-of-band views into the HTTP response.
    /// </summary>
    /// <param name="context">The action context in which the result is executed.</param>
    /// <returns>A task that represents the asynchronous execution operation.</returns>
    public async Task ExecuteResultAsync(ActionContext context)
    {
        var response = context.HttpContext.Response;
        response.ContentType = "text/html";

        var writer = new StringWriter();

        // Render main view without OOB wrapping
        if (_main is not null)
        {
            string mainHtml = await RenderViewSmart(context, _main);
            writer.WriteLine(mainHtml.Trim());
        }

        // Render OOB views
        foreach (var htmxViewInfo in _oobs)
        {
            string html = await RenderViewSmart(context, htmxViewInfo);
            string wrapped = AddHxSwapToOuterElement(html.Trim(), htmxViewInfo);
            writer.WriteLine(wrapped);
        }

        await response.WriteAsync(writer.ToString());
    }

    private static Task<string> RenderViewSmart(ActionContext context, HtmxViewInfo oobViewInfo)
    {
        return IsViewComponent(context, oobViewInfo.ViewName)
            ? RenderViewComponentToString(context, oobViewInfo)
            : RenderPartialViewToString(context, oobViewInfo);
    }


    private static bool IsViewComponent(ActionContext context, string viewName)
    {
        // Try to resolve the view name to a view component
        try
        {
            var viewComponentSelector = context.HttpContext.RequestServices.GetRequiredService<IViewComponentSelector>();
            return viewComponentSelector.SelectComponent(viewName) != null;
        }
        catch
        {
            // If resolving fails, it's a regular view
            return false;
        }
    }

    private static string AddHxSwapToOuterElement(string html, HtmxViewInfo htmxViewInfo)
    {
        // Use a regex to identify the outermost tag and add hx-swap-oob="true" to it
        var regex = new Regex(@"<(\w+)([^>]*)>");
        var match = regex.Match(html);

        var targetDisposition = htmxViewInfo.TargetDisposition switch
        {
            OobTargetDisposition.OuterHtml => "outerHTML",
            OobTargetDisposition.InnerHtml => "innerHTML",
            OobTargetDisposition.AfterBegin => "afterbegin",
            OobTargetDisposition.BeforeEnd => "beforeend",
            OobTargetDisposition.BeforeBegin => "beforebegin",
            OobTargetDisposition.AfterEnd => "afterend",
            OobTargetDisposition.Delete => "delete",
            OobTargetDisposition.None => "none",
            _ => throw new ArgumentOutOfRangeException(nameof(htmxViewInfo.TargetDisposition), "Invalid target disposition")
        };
        var targetSelector = "";
        
        if (!string.IsNullOrWhiteSpace(htmxViewInfo.TargetSelector))
        {
            if (!Regex.IsMatch(htmxViewInfo.TargetSelector, @"^[a-zA-Z0-9\-_#.: \[\]=]*$"))
            {
                throw new ArgumentException("TargetSelector contains invalid characters for a CSS query selector.");
            }
            targetSelector = ":" + htmxViewInfo.TargetSelector;
        }

        if (match.Success)
        {
            // Check if the outermost tag already contains hx-swap-oob
            if (!match.Value.Contains("hx-swap-oob"))
            {
                var tagName = match.Groups[1].Value;
                var tagAttributes = match.Groups[2].Value;

                // Add the hx-swap-oob attribute to the outermost element's tag
                var updatedTag = $"<{tagName}{tagAttributes} hx-swap-oob=\"{targetDisposition}{targetSelector}\">";

                // Replace the opening tag with the updated one
                return $"<template>{regex.Replace(html, updatedTag, 1)}</template>";
                //return regex.Replace(html, updatedTag, 1);
            }
        }

        return html;
    }

    private static async Task<string> RenderPartialViewToString(ActionContext context, HtmxViewInfo htmxViewInfo)
    {
        var httpContext = context.HttpContext;
        var controller = context.RouteData.Values["controller"]?.ToString();
        var viewEngine = httpContext.RequestServices.GetService<ICompositeViewEngine>()!;
        var tempDataProvider = httpContext.RequestServices.GetService<ITempDataProvider>()!;

        var viewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary())
        {
            Model = htmxViewInfo.Model
        };

        using var sw = new StringWriter();
        var viewResult = viewEngine.FindView(context, htmxViewInfo.ViewName, false);
        if (viewResult.View == null)
        {
            throw new InvalidOperationException($"The partial view '{htmxViewInfo.ViewName}' was not found. Searched locations: {string.Join(", ", viewResult.SearchedLocations ?? Enumerable.Empty<string>())}");
        }
        var tempData = new TempDataDictionary(httpContext, tempDataProvider);
        var viewContext = new ViewContext(context, viewResult.View, viewData, tempData, sw, new HtmlHelperOptions());

        await viewResult.View.RenderAsync(viewContext);
        return sw.ToString();
    }

    private static async Task<string> RenderViewComponentToString(ActionContext context, HtmxViewInfo htmxViewInfo)
    {
        var httpContext = context.HttpContext;

        var viewComponentHelper = httpContext.RequestServices.GetRequiredService<IViewComponentHelper>();

        // Contextualize the helper so it knows about the current request
        if (viewComponentHelper is IViewContextAware viewContextAware)
        {
            var viewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary())
            {
                Model = null // you can pass in a real model if needed
            };

            var tempDataProvider = httpContext.RequestServices.GetRequiredService<ITempDataProvider>();
            var tempData = new TempDataDictionary(httpContext, tempDataProvider);

            using var sw = new StringWriter();
            var viewContext = new ViewContext(context, NullView.Instance, viewData, tempData, sw, new HtmlHelperOptions());

            viewContextAware.Contextualize(viewContext);

            var content = await viewComponentHelper.InvokeAsync(htmxViewInfo.ViewName, htmxViewInfo.Model);
            content.WriteTo(sw, HtmlEncoder.Default);
            return sw.ToString();
        }

        throw new InvalidOperationException("ViewComponentHelper does not implement IViewContextAware.");
    }

}



internal class NullView : IView
{
    public static readonly NullView Instance = new();
    public string Path => "NullView";

    public Task RenderAsync(ViewContext context) => Task.CompletedTask;
}