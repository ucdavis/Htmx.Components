using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Htmx.Components.TagHelpers;

/// <summary>
/// Tag helper that includes the Htmx.Components client runtime.
/// </summary>
[HtmlTargetElement("htmx-scripts")]
public class HtmxScriptsTagHelper : TagHelper
{
    private const string RuntimePath = "/_content/Htmx.Components/js/htmx-components.js";
    private readonly IFileVersionProvider _fileVersionProvider;

    /// <summary>
    /// Gets or sets which scripts to include. If null or empty, includes all scripts.
    /// Valid values: "page-state-headers", "table-inline-editing", "blur-save-coordination", "request-lifecycle", "error-handling", "authentication-retry"
    /// </summary>
    public string? Include { get; set; }

    /// <summary>
    /// Gets or sets which scripts to exclude from the default set.
    /// Valid values: "page-state-headers", "table-inline-editing", "blur-save-coordination", "request-lifecycle", "error-handling", "authentication-retry"
    /// </summary>
    public string? Exclude { get; set; }

    /// <summary>
    /// Gets or sets the current view context used to resolve versioned static asset URLs.
    /// </summary>
    [ViewContext]
    [HtmlAttributeNotBound]
    public ViewContext ViewContext { get; set; } = null!;

    /// <summary>
    /// Initializes a new instance of the HtmxScriptsTagHelper with the required dependencies.
    /// </summary>
    /// <param name="fileVersionProvider">The static asset file version provider.</param>
    public HtmxScriptsTagHelper(IFileVersionProvider fileVersionProvider)
    {
        _fileVersionProvider = fileVersionProvider;
    }

    /// <summary>
    /// Processes the tag helper and renders the requested JavaScript behaviors.
    /// </summary>
    public override Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
    {
        output.TagName = null;
        var config = JsonSerializer.Serialize(new HtmxComponentsRuntimeConfig(GetScriptsToInclude().ToArray()));
        var runtimeUrl = _fileVersionProvider.AddFileVersionToPath(
            ViewContext.HttpContext.Request.PathBase,
            RuntimePath);

        output.Content.SetHtmlContent($"""
            <script type="application/json" id="htmx-components-config">{config}</script>
            <script src="{runtimeUrl}" defer></script>
            """);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Determines which scripts to include based on Include and Exclude properties.
    /// </summary>
    private IEnumerable<string> GetScriptsToInclude()
    {
        var allScripts = new[]
        {
            "page-state-headers",
            "table-inline-editing",
            "blur-save-coordination",
            "request-lifecycle",
            "error-handling",
            "authentication-retry"
        };

        // If Include is specified, only include those
        if (!string.IsNullOrWhiteSpace(Include))
        {
            var includeList = Include.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim())
                .Select(MapScriptName)
                .Where(s => s != null)
                .ToHashSet();

            return allScripts.Where(s => includeList.Contains(s));
        }

        // Start with all scripts
        var scripts = allScripts.AsEnumerable();

        // Remove excluded scripts
        if (!string.IsNullOrWhiteSpace(Exclude))
        {
            var excludeList = Exclude.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim())
                .Select(MapScriptName)
                .Where(s => s != null)
                .ToHashSet();

            scripts = scripts.Where(s => !excludeList.Contains(s));
        }

        return scripts;
    }

    /// <summary>
    /// Maps user-friendly script names to partial view names.
    /// </summary>
    private static string? MapScriptName(string userFriendlyName)
    {
        return userFriendlyName.ToLowerInvariant() switch
        {
            "page-state-headers" => "page-state-headers",
            "table-inline-editing" => "table-inline-editing",
            "blur-save-coordination" => "blur-save-coordination",
            "request-lifecycle" => "request-lifecycle",
            "error-handling" => "error-handling",
            "authentication-retry" => "authentication-retry",
            _ => null
        };
    }

    private sealed record HtmxComponentsRuntimeConfig(string[] Scripts);
}
