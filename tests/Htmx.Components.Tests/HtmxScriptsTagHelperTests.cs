using Htmx.Components.TagHelpers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Htmx.Components.Tests;

public class HtmxScriptsTagHelperTests
{
    [Fact]
    public async Task ProcessAsync_EmitsConfigJsonAndVersionedRuntimeReference()
    {
        var output = await RenderAsync(new HtmxScriptsTagHelper(new StubFileVersionProvider()));
        var html = output.Content.GetContent();

        Assert.Null(output.TagName);
        Assert.Contains("""<script type="application/json" id="htmx-components-config">""", html);
        Assert.Contains("\"/_content/Htmx.Components/js/htmx-components.js?v=test\"", html);
        Assert.Contains("page-state-headers", html);
        Assert.Contains("table-inline-editing", html);
        Assert.Contains("blur-save-coordination", html);
        Assert.Contains("request-lifecycle", html);
        Assert.Contains("error-handling", html);
        Assert.Contains("authentication-retry", html);
    }

    [Fact]
    public async Task ProcessAsync_HonorsIncludeAndExcludeConfiguration()
    {
        var output = await RenderAsync(new HtmxScriptsTagHelper(new StubFileVersionProvider())
        {
            Include = "page-state-headers, table-inline-editing",
            Exclude = "page-state-headers"
        });
        var html = output.Content.GetContent();

        Assert.DoesNotContain("page-state-headers", html);
        Assert.Contains("table-inline-editing", html);
        Assert.DoesNotContain("blur-save-coordination", html);
        Assert.DoesNotContain("error-handling", html);
        Assert.DoesNotContain("authentication-retry", html);
    }

    private static async Task<TagHelperOutput> RenderAsync(HtmxScriptsTagHelper tagHelper)
    {
        tagHelper.ViewContext = new ViewContext
        {
            HttpContext = new DefaultHttpContext()
        };

        var output = new TagHelperOutput(
            "htmx-scripts",
            [],
            (_, _) => Task.FromResult<TagHelperContent>(new DefaultTagHelperContent()));

        await tagHelper.ProcessAsync(
            new TagHelperContext([], new Dictionary<object, object>(), "test"),
            output);

        return output;
    }

    private sealed class StubFileVersionProvider : IFileVersionProvider
    {
        public string AddFileVersionToPath(PathString requestPathBase, string path)
            => $"{requestPathBase}{path}?v=test";
    }
}
