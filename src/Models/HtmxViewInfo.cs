namespace Htmx.Components.Models;

/// <summary>
/// Contains information needed to render a view in an HTMX context, including out-of-band targeting details.
/// This class encapsulates the view name, model data, and HTMX-specific rendering instructions.
/// </summary>
public class HtmxViewInfo
{
    /// <summary>
    /// Gets or sets the name of the view to render.
    /// </summary>
    public string ViewName { get; set; } = "";
    
    /// <summary>
    /// Gets or sets the model object to pass to the view during rendering.
    /// </summary>
    public object Model { get; set; } = null!;
    
    /// <summary>
    /// Gets or sets the out-of-band target disposition that determines how the rendered content should be handled by HTMX.
    /// </summary>
    public OobTargetDisposition TargetDisposition { get; set; } = OobTargetDisposition.None;
    
    /// <summary>
    /// Gets or sets the CSS selector for targeting specific elements when using out-of-band updates.
    /// This property is only used when TargetDisposition is not None.
    /// </summary>
    public string? TargetSelector { get; set; } = null;
}

