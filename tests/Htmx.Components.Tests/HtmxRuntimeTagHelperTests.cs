using Htmx.Components.TagHelpers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Htmx.Components.Tests;

public class HtmxRuntimeTagHelperTests
{
    [Fact]
    public async Task ProcessAsync_EmitsConfigJsonAndVersionedRuntimeReference()
    {
        var output = await RenderAsync(new HtmxRuntimeTagHelper(new StubFileVersionProvider()));
        var html = output.Content.GetContent();

        Assert.Null(output.TagName);
        Assert.Contains("""<script type="application/json" id="htmx-components-runtime-config">""", html);
        Assert.Contains("\"behaviors\":[", html);
        Assert.Contains("\"/_content/Htmx.Components/js/htmx-components.js?v=test\"", html);
        Assert.Contains("page-state-headers", html);
        Assert.Contains("table-inline-editing", html);
        Assert.Contains("blur-save-coordination", html);
        Assert.Contains("request-lifecycle", html);
        Assert.Contains("error-handling", html);
        Assert.Contains("authentication-retry", html);
        Assert.Contains("modal", html);
    }

    [Fact]
    public async Task ProcessAsync_HonorsIncludeBehaviorsAndExcludeBehaviorsConfiguration()
    {
        var output = await RenderAsync(new HtmxRuntimeTagHelper(new StubFileVersionProvider())
        {
            IncludeBehaviors = "page-state-headers, table-inline-editing",
            ExcludeBehaviors = "page-state-headers"
        });
        var html = output.Content.GetContent();

        Assert.DoesNotContain("page-state-headers", html);
        Assert.Contains("table-inline-editing", html);
        Assert.DoesNotContain("blur-save-coordination", html);
        Assert.DoesNotContain("error-handling", html);
        Assert.DoesNotContain("authentication-retry", html);
        Assert.DoesNotContain("modal", html);
    }

    [Fact]
    public async Task ProcessAsync_ThrowsForUnknownBehavior()
    {
        var tagHelper = new HtmxRuntimeTagHelper(new StubFileVersionProvider())
        {
            IncludeBehaviors = "request-lifecycle, typo"
        };

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => RenderAsync(tagHelper));

        Assert.Contains("Unknown Htmx.Components runtime behavior 'typo'", exception.Message);
    }

    private static async Task<TagHelperOutput> RenderAsync(HtmxRuntimeTagHelper tagHelper)
    {
        tagHelper.ViewContext = new ViewContext
        {
            HttpContext = new DefaultHttpContext()
        };

        var output = new TagHelperOutput(
            "htmx-runtime",
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
