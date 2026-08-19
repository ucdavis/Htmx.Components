using Microsoft.AspNetCore.Html;

namespace Htmx.Components.Modal.Models;

/// <summary>
/// Represents a reusable DaisyUI dialog shell for HTMX-loaded modal content.
/// </summary>
public class ModalModel
{
    /// <summary>
    /// Gets or sets the stable page-scoped dialog id.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Gets or sets the accessible modal title.
    /// </summary>
    public required string Title { get; init; }

    /// <summary>
    /// Gets or sets the modal size.
    /// </summary>
    public ModalSize Size { get; init; } = ModalSize.Default;

    /// <summary>
    /// Gets or sets the stable body target id used by HTMX responses.
    /// </summary>
    public string? BodyTargetId { get; init; }

    /// <summary>
    /// Gets or sets the accessible label used by explicit close controls.
    /// </summary>
    public string CloseLabel { get; init; } = "Close";

    /// <summary>
    /// Gets or sets optional initial modal body content.
    /// </summary>
    public IHtmlContent? Body { get; init; }

    /// <summary>
    /// Gets the effective modal body target id.
    /// </summary>
    public string EffectiveBodyTargetId => string.IsNullOrWhiteSpace(BodyTargetId)
        ? $"{Id}-body"
        : BodyTargetId;

    /// <summary>
    /// Gets the title element id used to label the native dialog.
    /// </summary>
    public string TitleId => $"{Id}-title";

    /// <summary>
    /// Gets the DaisyUI modal-box size class for the selected size.
    /// </summary>
    public string SizeClass => Size switch
    {
        ModalSize.Small => "max-w-md",
        ModalSize.Large => "max-w-4xl",
        ModalSize.ExtraLarge => "max-w-6xl",
        ModalSize.Full => "max-w-full w-11/12",
        _ => string.Empty
    };
}
