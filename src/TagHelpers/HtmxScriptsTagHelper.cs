using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Htmx.Components.TagHelpers;

/// <summary>
/// Tag helper that includes all Htmx.Components JavaScript behaviors as inline scripts.
/// This replaces the need to include individual JavaScript files and allows for dynamic content generation.
/// </summary>
[HtmlTargetElement("htmx-scripts")]
public class HtmxScriptsTagHelper : TagHelper
{
    private readonly IHtmlHelper _htmlHelper;
    private readonly IWebHostEnvironment _environment;

    /// <summary>
    /// Gets or sets which scripts to include. If null or empty, includes all scripts.
    /// Valid values: "page-state-headers", "table-inline-editing", "blur-save-coordination", "authentication-retry"
    /// </summary>
    public string? Include { get; set; }

    /// <summary>
    /// Gets or sets which scripts to exclude from the default set.
    /// Valid values: "page-state-headers", "table-inline-editing", "blur-save-coordination", "authentication-retry"
    /// </summary>
    public string? Exclude { get; set; }

    /// <summary>
    /// ViewContext is required to contextualize the IHtmlHelper
    /// </summary>
    [ViewContext]
    [HtmlAttributeNotBound]
    public ViewContext ViewContext { get; set; } = null!;

    /// <summary>
    /// Initializes a new instance of the HtmxScriptsTagHelper with the required dependencies.
    /// </summary>
    /// <param name="htmlHelper">The HTML helper for rendering HTML content</param>
    /// <param name="environment">The web hosting environment for accessing application information</param>
    public HtmxScriptsTagHelper(IHtmlHelper htmlHelper, IWebHostEnvironment environment)
    {
        _htmlHelper = htmlHelper;
        _environment = environment;
    }

    /// <summary>
    /// Processes the tag helper and renders the requested JavaScript behaviors.
    /// </summary>
    public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
    {
        // Contextualize the HTML helper
        ((IViewContextAware)_htmlHelper).Contextualize(ViewContext);

        // Change the tag to a script tag
        output.TagName = "script";
        output.Attributes.SetAttribute("type", "text/javascript");

        var scriptsToInclude = GetScriptsToInclude();
        var scriptContent = new List<string>();

        foreach (var script in scriptsToInclude)
        {
            try
            {
                var content = await _htmlHelper.PartialAsync($"Scripts/_{script}");
                if (content != null)
                {
                    // Use StringWriter to properly extract the content from IHtmlContent
                    using var writer = new StringWriter();
                    content.WriteTo(writer, HtmlEncoder.Default);
                    var scriptText = writer.ToString();
                    if (!string.IsNullOrWhiteSpace(scriptText))
                    {
                        scriptContent.Add(scriptText);
                    }
                }
            }
            catch (Exception ex)
            {
                // Capture any exception for debugging
                scriptContent.Add($"// Error loading {script}: {ex.Message}");
                continue;
            }
        }

        if (scriptContent.Any())
        {
            var combinedScript = string.Join("\n\n", scriptContent);
            output.Content.SetHtmlContent($"\n{combinedScript}\n");
        }
        else
        {
            // For debugging: output which scripts were attempted
            var attempted = string.Join(", ", scriptsToInclude);
            output.Content.SetHtmlContent($"\n// No scripts found. Attempted: {attempted}\n");
        }
    }

    /// <summary>
    /// Determines which scripts to include based on Include and Exclude properties.
    /// </summary>
    private IEnumerable<string> GetScriptsToInclude()
    {
        var allScripts = new[]
        {
            "PageStateHeaders",
            "TableInlineEditing", 
            "BlurSaveCoordination",
            "AuthenticationRetry"
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
            "page-state-headers" => "PageStateHeaders",
            "table-inline-editing" => "TableInlineEditing",
            "blur-save-coordination" => "BlurSaveCoordination", 
            "authentication-retry" => "AuthenticationRetry",
            _ => null
        };
    }
}
