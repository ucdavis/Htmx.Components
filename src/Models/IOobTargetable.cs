namespace Htmx.Components.Models;

/// <summary>
/// Represents a model that can provide targeting information for HTMX out-of-band (OOB) updates.
/// Models implementing this interface can specify where and how their rendered content should be placed in the DOM.
/// </summary>
public interface IOobTargetable
{
    /// <summary>
    /// Gets the CSS selector that identifies the target element for the out-of-band update.
    /// </summary>
    string? TargetSelector { get; }
    
    /// <summary>
    /// Gets the disposition that determines how the rendered content should be positioned relative to the target element.
    /// </summary>
    OobTargetDisposition? TargetDisposition { get; }
}

/// <summary>
/// Specifies how content should be positioned when performing HTMX out-of-band updates.
/// These values correspond to HTMX's OOB swap strategies for DOM manipulation.
/// </summary>
public enum OobTargetDisposition
{
    /// <summary>
    /// Replaces the entire target element with the new content (hx-swap="outerHTML").
    /// </summary>
    OuterHtml,
    
    /// <summary>
    /// Replaces the content inside the target element (hx-swap="innerHTML").
    /// </summary>
    InnerHtml,
    
    /// <summary>
    /// Inserts the content as the first child of the target element (hx-swap="afterbegin").
    /// </summary>
    AfterBegin,
    
    /// <summary>
    /// Inserts the content as the last child of the target element (hx-swap="beforeend").
    /// </summary>
    BeforeEnd,
    
    /// <summary>
    /// Inserts the content immediately before the target element (hx-swap="beforebegin").
    /// </summary>
    BeforeBegin,
    
    /// <summary>
    /// Inserts the content immediately after the target element (hx-swap="afterend").
    /// </summary>
    AfterEnd,
    
    /// <summary>
    /// No out-of-band update should be performed.
    /// </summary>
    None,
    
    /// <summary>
    /// Removes the target element from the DOM (hx-swap="delete").
    /// </summary>
    Delete,
}