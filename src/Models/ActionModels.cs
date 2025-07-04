namespace Htmx.Components.Models;

/// <summary>
/// Represents a visual item that can be displayed with a label, icon, and CSS styling.
/// This interface forms the base for all actionable UI elements in the component system.
/// </summary>
public interface IActionItem
{
    /// <summary>
    /// Gets or sets the display text for the action item.
    /// </summary>
    string Label { get; set; }
    
    /// <summary>
    /// Gets or sets the CSS icon class for the action item (e.g., "fas fa-edit").
    /// </summary>
    string? Icon { get; set; }
    
    /// <summary>
    /// Gets or sets the CSS class to apply to the action item for styling.
    /// </summary>
    string? CssClass { get; set; }
}

/// <summary>
/// Represents a container for grouping multiple action items without display properties.
/// This interface is useful for creating menus, toolbars, or navigation bars where
/// the container itself doesn't need visual representation.
/// </summary>
public interface IActionSet
{
    /// <summary>
    /// Gets or sets the collection of action items contained in this set.
    /// </summary>
    public List<IActionItem> Items { get; set; }
}

/// <summary>
/// A basic implementation of an action item with label, icon, and CSS class properties.
/// This class provides the fundamental display properties for actionable UI elements.
/// </summary>
public class ActionItem: IActionItem
{
    /// <summary>
    /// Gets or sets the display text for the action item.
    /// </summary>
    public string Label { get; set; } = "";
    
    /// <summary>
    /// Gets or sets the CSS icon class for the action item.
    /// </summary>
    public string? Icon { get; set; }
    
    /// <summary>
    /// Gets or sets the CSS class to apply to the action item for styling.
    /// </summary>
    public string? CssClass { get; set; }
}

/// <summary>
/// A container for grouping multiple action items without its own display properties.
/// This class is useful for creating menus, toolbars, or navigation bars where
/// the container itself doesn't need visual representation.
/// </summary>
public class ActionSet : IActionSet
{
    /// <summary>
    /// Gets or sets the collection of action items contained in this set.
    /// </summary>
    public List<IActionItem> Items { get; set; }

    /// <summary>
    /// Initializes a new instance of the ActionSet class with the specified configuration.
    /// </summary>
    /// <param name="config">The configuration object containing the items for this set.</param>
    internal ActionSet(ActionSetConfig config)
    {
        Items = config.Items;
    }

    /// <summary>
    /// Initializes a new instance of the ActionSet class with an empty item collection.
    /// </summary>
    public ActionSet() : this(new ActionSetConfig()) { }
}

/// <summary>
/// An action item that can contain nested action items, combining individual item properties
/// with container functionality. This class is useful for creating dropdown menus or
/// hierarchical navigation structures.
/// </summary>
public class ActionGroup : ActionItem, IActionSet
{
    /// <summary>
    /// Gets or sets the collection of nested action items contained in this group.
    /// </summary>
    public List<IActionItem> Items { get; set; }

    /// <summary>
    /// Initializes a new instance of the ActionGroup class with the specified configuration.
    /// </summary>
    /// <param name="config">The configuration object containing the group properties and items.</param>
    internal ActionGroup(ActionGroupConfig config)
    {
        Label = config.Label;
        Icon = config.Icon;
        CssClass = config.CssClass;
        Items = config.Items;
    }

    /// <summary>
    /// Initializes a new instance of the ActionGroup class with default properties.
    /// </summary>
    public ActionGroup() : this(new ActionGroupConfig()) { }
}

/// <summary>
/// An action item with additional attributes support, primarily designed for HTMX attributes.
/// This class extends the basic action item with a dictionary of custom attributes and active state tracking.
/// </summary>
public class ActionModel : ActionItem
{
    /// <summary>
    /// Gets the custom attributes to apply to the action element, typically used for HTMX attributes.
    /// </summary>
    public Dictionary<string, string> Attributes { get; init; } = new();
    
    /// <summary>
    /// Gets or sets a value indicating whether this action is currently active or selected.
    /// </summary>
    public bool IsActive { get; set; } = false;

    /// <summary>
    /// Initializes a new instance of the ActionModel class with the specified configuration.
    /// </summary>
    /// <param name="config">The configuration object containing the action properties and attributes.</param>
    internal ActionModel(ActionModelConfig config)
    {
        Label = config.Label;
        Icon = config.Icon;
        CssClass = config.CssClass;
        Attributes = new Dictionary<string, string>(config.Attributes);
        IsActive = config.IsActive;
    }
}

/// <summary>
/// Internal configuration class for ActionModel initialization.
/// This class is used internally by builders to configure ActionModel instances.
/// </summary>
internal class ActionModelConfig
{
    /// <summary>
    /// Gets or sets the display text for the action.
    /// </summary>
    public string Label { get; set; } = "";
    
    /// <summary>
    /// Gets or sets the CSS icon class for the action.
    /// </summary>
    public string? Icon { get; set; }
    
    /// <summary>
    /// Gets or sets the CSS class to apply to the action for styling.
    /// </summary>
    public string? CssClass { get; set; }
    
    /// <summary>
    /// Gets or sets the custom attributes to apply to the action element.
    /// </summary>
    public Dictionary<string, string> Attributes { get; set; } = new();
    
    /// <summary>
    /// Gets or sets a value indicating whether this action is currently active.
    /// </summary>
    public bool IsActive { get; set; }
}

/// <summary>
/// Configuration class for ActionSet initialization.
/// This class is used by builders to configure ActionSet instances and cannot be marked 
/// as internal because it is used as a type constraint in ActionItemsBuilder.
/// </summary>
public class ActionSetConfig
{
    /// <summary>
    /// Gets or sets the collection of action items for the set.
    /// </summary>
    public List<IActionItem> Items { get; set; } = new();
}

/// <summary>
/// Configuration class for ActionGroup initialization.
/// This class extends ActionSetConfig with display properties and cannot be marked 
/// as internal because the base class is used as a type constraint in ActionItemsBuilder.
/// </summary>
public class ActionGroupConfig : ActionSetConfig
{
    /// <summary>
    /// Gets or sets the display text for the action group.
    /// </summary>
    public string Label { get; set; } = "";
    
    /// <summary>
    /// Gets or sets the CSS icon class for the action group.
    /// </summary>
    public string? Icon { get; set; }
    
    /// <summary>
    /// Gets or sets the CSS class to apply to the action group for styling.
    /// </summary>
    public string? CssClass { get; set; }
}