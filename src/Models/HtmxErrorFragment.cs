using System.Text.Encodings.Web;

namespace Htmx.Components.Models;

/// <summary>
/// Describes a user-safe error fragment that the client runtime can place in an error region.
/// </summary>
public sealed record HtmxErrorFragment(
    string Title,
    string Message,
    string Kind = HtmxErrorKinds.General,
    string? ComponentId = null)
{
    /// <summary>
    /// Renders the fragment as HTML with structured attributes for the client runtime.
    /// </summary>
    public string ToHtml()
    {
        var encoder = HtmlEncoder.Default;
        var componentAttribute = string.IsNullOrWhiteSpace(ComponentId)
            ? ""
            : $" data-hc-component-id=\"{encoder.Encode(ComponentId)}\"";

        return $"""
            <div data-hc-error-fragment data-hc-error-kind="{encoder.Encode(Kind)}"{componentAttribute}>
                <strong data-hc-error-title>{encoder.Encode(Title)}</strong>
                <span data-hc-error-message>{encoder.Encode(Message)}</span>
            </div>
            """;
    }
}

/// <summary>
/// Well-known HTMX error categories.
/// </summary>
public static class HtmxErrorKinds
{
    /// <summary>
    /// General request failure.
    /// </summary>
    public const string General = "general";

    /// <summary>
    /// User input or validation failure.
    /// </summary>
    public const string Validation = "validation";

    /// <summary>
    /// Authentication or authorization failure.
    /// </summary>
    public const string Authorization = "authorization";

    /// <summary>
    /// Missing model handler or unavailable action failure.
    /// </summary>
    public const string MissingHandler = "missing-handler";

    /// <summary>
    /// CRUD operation failure.
    /// </summary>
    public const string Crud = "crud";
}
