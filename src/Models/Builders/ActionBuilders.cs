using Htmx.Components.Models;

namespace Htmx.Components.Models.Builders;

/// <summary>
/// Abstract base class for builders that create action item collections.
/// Provides common functionality for building sets of actions (buttons, links, etc.) with fluent configuration.
/// </summary>
/// <typeparam name="TBuilder">The concrete builder type for fluent chaining</typeparam>
/// <typeparam name="TSet">The type of action set being built</typeparam>
/// <typeparam name="TConfig">The configuration type used to store build settings</typeparam>
public abstract class ActionItemsBuilder<TBuilder, TSet, TConfig> : BuilderBase<TBuilder, TSet>
    where TBuilder : ActionItemsBuilder<TBuilder, TSet, TConfig>
    where TSet : class, IActionSet, new()
    where TConfig : ActionSetConfig, new()
{
    /// <summary>
    /// The configuration object that stores settings for building the action set.
    /// Contains all the accumulated configuration from builder method calls.
    /// </summary>
    protected readonly TConfig _config = new();

    /// <summary>
    /// Initializes a new instance of the ActionItemsBuilder with the specified service provider.
    /// </summary>
    /// <param name="serviceProvider">The service provider for dependency injection</param>
    protected ActionItemsBuilder(IServiceProvider serviceProvider)
        : base(serviceProvider) { }

    /// <summary>
    /// Adds a new action to the collection using a fluent configuration delegate.
    /// This allows for detailed customization of individual action items.
    /// </summary>
    /// <param name="configure">A delegate that configures the action using an ActionModelBuilder</param>
    /// <returns>The current builder instance for method chaining</returns>
    public TBuilder AddAction(Action<ActionModelBuilder> configure)
    {
        AddBuildTask(async () =>
        {
            var actionModelBuilder = new ActionModelBuilder(ServiceProvider);
            configure(actionModelBuilder);
            var actionModel = await actionModelBuilder.BuildAsync();
            _config.Items.Add(actionModel);
        });
        return (TBuilder)this;
    }

    /// <summary>
    /// Adds a pre-configured action item directly to the collection.
    /// This method is useful when you have an existing IActionItem instance to add.
    /// </summary>
    /// <param name="item">The action item to add to the collection</param>
    /// <returns>The current builder instance for method chaining</returns>
    public TBuilder AddItem(IActionItem item)
    {
        _config.Items.Add(item);
        return (TBuilder)this;
    }

    /// <summary>
    /// Adds multiple pre-configured action items to the collection.
    /// This method allows for bulk addition of action items from another source.
    /// </summary>
    /// <param name="items">The collection of action items to add</param>
    /// <returns>The current builder instance for method chaining</returns>
    public TBuilder AddRange(IEnumerable<IActionItem> items)
    {
        _config.Items.AddRange(items);
        return (TBuilder)this;
    }
}

/// <summary>
/// Concrete builder for creating ActionSet instances.
/// Provides functionality to build collections of actions and action groups with fluent configuration.
/// </summary>
public class ActionSetBuilder : ActionItemsBuilder<ActionSetBuilder, ActionSet, ActionSetConfig>
{
    /// <summary>
    /// Initializes a new instance of the ActionSetBuilder with the specified service provider.
    /// </summary>
    /// <param name="serviceProvider">The service provider for dependency injection</param>
    public ActionSetBuilder(IServiceProvider serviceProvider)
        : base(serviceProvider) { }

    /// <summary>
    /// Adds a new action group to the set using a fluent configuration delegate.
    /// Action groups provide logical grouping of related actions with optional styling and labels.
    /// </summary>
    /// <param name="configure">A delegate that configures the action group using an ActionGroupBuilder</param>
    /// <returns>The current builder instance for method chaining</returns>
    public ActionSetBuilder AddGroup(Action<ActionGroupBuilder> configure)
    {
        AddBuildTask(async () =>
        {
            var actionGroupBuilder = new ActionGroupBuilder(ServiceProvider);
            configure(actionGroupBuilder);
            var actionGroup = await actionGroupBuilder.BuildAsync();
            _config.Items.Add(actionGroup);
        });
        return this;
    }

    /// <summary>
    /// Builds the final ActionSet instance with all configured actions and groups.
    /// This method is called internally to create the final result.
    /// </summary>
    /// <returns>A task that represents the asynchronous build operation, containing the built ActionSet</returns>
    protected override Task<ActionSet> BuildImpl()
        => Task.FromResult(new ActionSet(_config));
}

/// <summary>
/// Builder for creating ActionGroup instances with optional labeling, styling, and contained actions.
/// Action groups provide logical organization of related actions with visual grouping.
/// </summary>
public class ActionGroupBuilder : ActionItemsBuilder<ActionGroupBuilder, ActionGroup, ActionGroupConfig>
{
    /// <summary>
    /// Initializes a new instance of the ActionGroupBuilder with the specified service provider.
    /// </summary>
    /// <param name="serviceProvider">The service provider for dependency injection</param>
    public ActionGroupBuilder(IServiceProvider serviceProvider)
        : base(serviceProvider) { }

    /// <summary>
    /// Sets the display label for the action group.
    /// The label is typically shown as a header or title for the grouped actions.
    /// </summary>
    /// <param name="label">The text label to display for the group</param>
    /// <returns>The current builder instance for method chaining</returns>
    public ActionGroupBuilder WithLabel(string label)
    {
        _config.Label = label;
        return this;
    }

    /// <summary>
    /// Sets the icon for the action group.
    /// Icons provide visual identification and can be CSS classes or icon font references.
    /// </summary>
    /// <param name="icon">The icon identifier (CSS class, font icon, etc.)</param>
    /// <returns>The current builder instance for method chaining</returns>
    public ActionGroupBuilder WithIcon(string icon)
    {
        _config.Icon = icon;
        return this;
    }

    /// <summary>
    /// Sets CSS classes to apply to the action group container.
    /// Used for styling and layout customization of the group.
    /// </summary>
    /// <param name="cssClass">The CSS class names to apply</param>
    /// <returns>The current builder instance for method chaining</returns>
    public ActionGroupBuilder WithClass(string cssClass)
    {
        _config.CssClass = cssClass;
        return this;
    }

    /// <summary>
    /// Builds the final ActionGroup instance with all configured properties and actions.
    /// This method is called internally to create the final result.
    /// </summary>
    /// <returns>A task that represents the asynchronous build operation, containing the built ActionGroup</returns>
    protected override Task<ActionGroup> BuildImpl()
        => Task.FromResult(new ActionGroup(_config));
}

/// <summary>
/// Builder for creating individual ActionModel instances with detailed configuration.
/// Provides fluent configuration for buttons, links, and other interactive action elements.
/// </summary>
public class ActionModelBuilder : BuilderBase<ActionModelBuilder, ActionModel>
{
    private readonly ActionModelConfig _config = new();

    /// <summary>
    /// Initializes a new instance of the ActionModelBuilder with the specified service provider.
    /// </summary>
    /// <param name="serviceProvider">The service provider for dependency injection</param>
    public ActionModelBuilder(IServiceProvider serviceProvider)
        : base(serviceProvider) { }

    /// <summary>
    /// Sets the display label for the action.
    /// The label is the text shown to users for buttons, links, or menu items.
    /// </summary>
    /// <param name="label">The text label to display</param>
    /// <returns>The current builder instance for method chaining</returns>
    public ActionModelBuilder WithLabel(string label)
    {
        _config.Label = label;
        return this;
    }

    /// <summary>
    /// Sets the icon for the action.
    /// Icons provide visual identification and can be CSS classes or icon font references.
    /// </summary>
    /// <param name="icon">The icon identifier (CSS class, font icon, etc.)</param>
    /// <returns>The current builder instance for method chaining</returns>
    public ActionModelBuilder WithIcon(string icon)
    {
        _config.Icon = icon;
        return this;
    }

    /// <summary>
    /// Sets CSS classes to apply to the action element.
    /// Used for styling, theming, and layout customization.
    /// </summary>
    /// <param name="cssClass">The CSS class names to apply</param>
    /// <returns>The current builder instance for method chaining</returns>
    public ActionModelBuilder WithClass(string cssClass)
    {
        _config.CssClass = cssClass;
        return this;
    }

    /// <summary>
    /// Adds a custom HTML attribute to the action element.
    /// Useful for data attributes, accessibility properties, or custom behavior.
    /// </summary>
    /// <param name="name">The attribute name</param>
    /// <param name="value">The attribute value</param>
    /// <returns>The current builder instance for method chaining</returns>
    public ActionModelBuilder WithAttribute(string name, string value)
    {
        _config.Attributes[name] = value;
        return this;
    }

    /// <summary>
    /// Sets whether the action is currently active or selected.
    /// Active actions typically receive different styling to indicate current state.
    /// </summary>
    /// <param name="isActive">True if the action should be marked as active</param>
    /// <returns>The current builder instance for method chaining</returns>
    public ActionModelBuilder WithIsActive(bool isActive)
    {
        _config.IsActive = isActive;
        return this;
    }

    /// <summary>
    /// Sets the HTMX hx-get attribute for HTTP GET requests.
    /// </summary>
    /// <param name="url">The URL to request when the action is triggered</param>
    /// <returns>The current builder instance for method chaining</returns>
    public ActionModelBuilder WithHxGet(string url) => WithAttribute("hx-get", url);
    
    /// <summary>
    /// Sets the HTMX hx-post attribute for HTTP POST requests.
    /// </summary>
    /// <param name="url">The URL to request when the action is triggered</param>
    /// <returns>The current builder instance for method chaining</returns>
    public ActionModelBuilder WithHxPost(string url) => WithAttribute("hx-post", url);
    
    /// <summary>
    /// Sets the HTMX hx-target attribute to specify where response content should be placed.
    /// </summary>
    /// <param name="target">The CSS selector for the target element</param>
    /// <returns>The current builder instance for method chaining</returns>
    public ActionModelBuilder WithHxTarget(string target) => WithAttribute("hx-target", target);
    
    /// <summary>
    /// Sets the HTMX hx-swap attribute to specify how content should be swapped.
    /// </summary>
    /// <param name="swap">The swap strategy (innerHTML, outerHTML, afterbegin, etc.)</param>
    /// <returns>The current builder instance for method chaining</returns>
    public ActionModelBuilder WithHxSwap(string swap) => WithAttribute("hx-swap", swap);
    
    /// <summary>
    /// Sets the HTMX hx-push-url attribute to control browser history.
    /// </summary>
    /// <param name="pushUrl">The URL to push to browser history, or "false" to disable</param>
    /// <returns>The current builder instance for method chaining</returns>
    public ActionModelBuilder WithHxPushUrl(string pushUrl = "true") => WithAttribute("hx-push-url", pushUrl);
    
    /// <summary>
    /// Sets the HTMX hx-include attribute to include additional elements in requests.
    /// </summary>
    /// <param name="selector">The CSS selector for elements to include</param>
    /// <returns>The current builder instance for method chaining</returns>
    public ActionModelBuilder WithHxInclude(string selector) => WithAttribute("hx-include", selector);

    /// <summary>
    /// Builds the final ActionModel instance with all configured properties.
    /// This method is called internally to create the final result.
    /// </summary>
    /// <returns>A task that represents the asynchronous build operation, containing the built ActionModel</returns>
    protected override Task<ActionModel> BuildImpl()
    => Task.FromResult(new ActionModel(_config));
}