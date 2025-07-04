using Htmx.Components.Table.Models;

namespace Htmx.Components.Models;

/// <summary>
/// Contains the default view paths used by the Htmx Components framework.
/// This class centralizes the management of partial view paths for different component types,
/// allowing for easy customization of the framework's rendering behavior.
/// </summary>
public class ViewPaths
{
    /// <summary>
    /// Gets or sets the view paths used by table components.
    /// This provides access to all table-specific partial views including cells, filters, and actions.
    /// </summary>
    public TableViewPaths Table { get; set; } = new();
    
    /// <summary>
    /// Gets or sets the view path for the navigation bar component.
    /// Defaults to "Default" which corresponds to the standard navigation bar layout.
    /// </summary>
    public string NavBar { get; set; } = "Default";
    
    /// <summary>
    /// Gets or sets the view path for the authentication status component.
    /// Defaults to "Default" which shows the standard authentication status display.
    /// </summary>
    public string AuthStatus { get; set; } = "Default";
    
    /// <summary>
    /// Gets or sets the view path for input components.
    /// Defaults to "_Input" which is the standard input rendering partial view.
    /// </summary>
    public string Input { get; set; } = "_Input";
    
    /// <summary>
    /// Gets or sets the view path for the default navigation content.
    /// This is used when no custom navigation content is specified.
    /// </summary>
    public string DefaultNavContent { get; set; } = "_DefaultNavContent";
}