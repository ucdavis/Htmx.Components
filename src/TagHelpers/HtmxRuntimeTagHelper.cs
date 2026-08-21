using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Htmx.Components.TagHelpers;

/// <summary>
/// Tag helper that includes the Htmx.Components client runtime.
/// </summary>
[HtmlTargetElement("htmx-runtime")]
public class HtmxRuntimeTagHelper : TagHelper
{
    private const string RuntimePath = "/_content/Htmx.Components/js/htmx-components.js";
    private static readonly JsonSerializerOptions RuntimeConfigJsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly string[] AllBehaviors =
    [
        "page-state-headers",
        "table-inline-editing",
        "blur-save-coordination",
        "request-lifecycle",
        "error-handling",
        "authentication-retry",
        "modal"
    ];

    private readonly IFileVersionProvider _fileVersionProvider;

    /// <summary>
    /// Gets or sets which runtime behaviors to include. If null or empty, includes all behaviors.
    /// Valid values: "page-state-headers", "table-inline-editing", "blur-save-coordination", "request-lifecycle", "error-handling", "authentication-retry", "modal"
    /// </summary>
    [HtmlAttributeName("include-behaviors")]
    public string? IncludeBehaviors { get; set; }

    /// <summary>
    /// Gets or sets which runtime behaviors to exclude from the selected set.
    /// Valid values: "page-state-headers", "table-inline-editing", "blur-save-coordination", "request-lifecycle", "error-handling", "authentication-retry", "modal"
    /// </summary>
    [HtmlAttributeName("exclude-behaviors")]
    public string? ExcludeBehaviors { get; set; }

    /// <summary>
    /// Gets or sets the current view context used to resolve versioned static asset URLs.
    /// </summary>
    [ViewContext]
    [HtmlAttributeNotBound]
    public ViewContext ViewContext { get; set; } = null!;

    /// <summary>
    /// Initializes a new instance of the HtmxRuntimeTagHelper with the required dependencies.
    /// </summary>
    /// <param name="fileVersionProvider">The static asset file version provider.</param>
    public HtmxRuntimeTagHelper(IFileVersionProvider fileVersionProvider)
    {
        _fileVersionProvider = fileVersionProvider;
    }

    /// <summary>
    /// Processes the tag helper and renders the requested JavaScript behaviors.
    /// </summary>
    public override Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
    {
        output.TagName = null;
        var config = JsonSerializer.Serialize(
            new HtmxComponentsRuntimeConfig(GetBehaviorsToInclude().ToArray()),
            RuntimeConfigJsonOptions);
        var runtimeUrl = _fileVersionProvider.AddFileVersionToPath(
            ViewContext.HttpContext.Request.PathBase,
            RuntimePath);

        output.Content.SetHtmlContent($"""
            <script type="application/json" id="htmx-components-runtime-config">{config}</script>
            <script src="{runtimeUrl}" defer></script>
            """);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Determines which behaviors to include based on IncludeBehaviors and ExcludeBehaviors properties.
    /// </summary>
    private IEnumerable<string> GetBehaviorsToInclude()
    {
        IEnumerable<string> behaviors = AllBehaviors;

        if (!string.IsNullOrWhiteSpace(IncludeBehaviors))
        {
            var includeList = ParseBehaviorList(IncludeBehaviors, nameof(IncludeBehaviors))
                .ToHashSet();

            behaviors = behaviors.Where(includeList.Contains);
        }

        if (!string.IsNullOrWhiteSpace(ExcludeBehaviors))
        {
            var excludeList = ParseBehaviorList(ExcludeBehaviors, nameof(ExcludeBehaviors))
                .ToHashSet();

            behaviors = behaviors.Where(s => !excludeList.Contains(s));
        }

        return behaviors;
    }

    /// <summary>
    /// Maps and validates behavior names supplied through tag helper attributes.
    /// </summary>
    private static IEnumerable<string> ParseBehaviorList(string value, string propertyName)
    {
        foreach (var behavior in value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var normalized = behavior.ToLowerInvariant();
            if (!AllBehaviors.Contains(normalized))
            {
                throw new InvalidOperationException(
                    $"Unknown Htmx.Components runtime behavior '{behavior}' in {propertyName}. Valid values are: {string.Join(", ", AllBehaviors)}.");
            }

            yield return normalized;
        }
    }

    private sealed record HtmxComponentsRuntimeConfig(string[] Behaviors);
}
