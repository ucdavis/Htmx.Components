using Htmx.Components.State;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Htmx.Components.TagHelpers;

/// <summary>
/// A tag helper that renders the encrypted page state as a hidden input field within a container div.
/// This tag helper enables page state persistence across HTMX requests by embedding the state data in the HTML.
/// </summary>
/// <remarks>
/// The tag helper renders a div container with a hidden input field containing the encrypted page state.
/// The container can be targeted by HTMX for out-of-band updates to refresh the page state.
/// </remarks>
/// <example>
/// Usage in Razor views:
/// <code>
/// &lt;htmx-page-state container-id="my-state-container" /&gt;
/// </code>
/// </example>
[HtmlTargetElement("htmx-page-state")]
public class PageStateTagHelper : TagHelper
{
    private readonly IPageState _pageState;

    /// <summary>
    /// Initializes a new instance of the PageStateTagHelper class.
    /// </summary>
    /// <param name="pageState">The page state service that provides the encrypted state data.</param>
    public PageStateTagHelper(IPageState pageState)
    {
        _pageState = pageState;
    }

    /// <summary>
    /// Gets or sets the HTML ID for the container div element.
    /// This ID can be used for HTMX targeting and CSS styling.
    /// </summary>
    /// <value>The container div ID. Defaults to 'page_state_container'.</value>
    public string ContainerId { get; set; } = "page_state_container";

    /// <summary>
    /// Processes the tag helper and renders the page state container with a hidden input field.
    /// </summary>
    /// <param name="context">The tag helper context.</param>
    /// <param name="output">The tag helper output to modify.</param>
    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        // We want to render a <div> containing the hidden input
        output.TagName = "div";
        output.Attributes.SetAttribute("id", ContainerId);

        var serializedState = _pageState.Encrypted;

        output.Content.SetHtmlContent($@"
            <input type=""hidden"" id=""page_state"" name=""page_state"" value=""{serializedState}"">
        ");
    }
}
