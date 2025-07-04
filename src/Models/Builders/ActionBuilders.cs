using Htmx.Components.Models;

namespace Htmx.Components.Models.Builders;

public abstract class ActionItemsBuilder<TBuilder, TSet, TConfig> : BuilderBase<TBuilder, TSet>
    where TBuilder : ActionItemsBuilder<TBuilder, TSet, TConfig>
    where TSet : class, IActionSet, new()
    where TConfig : ActionSetConfig, new()
{
    protected readonly TConfig _config = new();

    protected ActionItemsBuilder(IServiceProvider serviceProvider)
        : base(serviceProvider) { }

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

    public TBuilder AddItem(IActionItem item)
    {
        _config.Items.Add(item);
        return (TBuilder)this;
    }

    public TBuilder AddRange(IEnumerable<IActionItem> items)
    {
        _config.Items.AddRange(items);
        return (TBuilder)this;
    }
}

public class ActionSetBuilder : ActionItemsBuilder<ActionSetBuilder, ActionSet, ActionSetConfig>
{
    public ActionSetBuilder(IServiceProvider serviceProvider)
        : base(serviceProvider) { }

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

    protected override Task<ActionSet> BuildImpl()
        => Task.FromResult(new ActionSet(_config));
}

public class ActionGroupBuilder : ActionItemsBuilder<ActionGroupBuilder, ActionGroup, ActionGroupConfig>
{
    public ActionGroupBuilder(IServiceProvider serviceProvider)
        : base(serviceProvider) { }

    public ActionGroupBuilder WithLabel(string label)
    {
        _config.Label = label;
        return this;
    }

    public ActionGroupBuilder WithIcon(string icon)
    {
        _config.Icon = icon;
        return this;
    }

    public ActionGroupBuilder WithClass(string cssClass)
    {
        _config.CssClass = cssClass;
        return this;
    }

    protected override Task<ActionGroup> BuildImpl()
        => Task.FromResult(new ActionGroup(_config));
}

public class ActionModelBuilder : BuilderBase<ActionModelBuilder, ActionModel>
{
    private readonly ActionModelConfig _config = new();

    public ActionModelBuilder(IServiceProvider serviceProvider)
        : base(serviceProvider) { }

    public ActionModelBuilder WithLabel(string label)
    {
        _config.Label = label;
        return this;
    }

    public ActionModelBuilder WithIcon(string icon)
    {
        _config.Icon = icon;
        return this;
    }

    public ActionModelBuilder WithClass(string cssClass)
    {
        _config.CssClass = cssClass;
        return this;
    }

    public ActionModelBuilder WithAttribute(string name, string value)
    {
        _config.Attributes[name] = value;
        return this;
    }

    public ActionModelBuilder WithIsActive(bool isActive)
    {
        _config.IsActive = isActive;
        return this;
    }

    public ActionModelBuilder WithHxGet(string url) => WithAttribute("hx-get", url);
    public ActionModelBuilder WithHxPost(string url) => WithAttribute("hx-post", url);
    public ActionModelBuilder WithHxTarget(string target) => WithAttribute("hx-target", target);
    public ActionModelBuilder WithHxSwap(string swap) => WithAttribute("hx-swap", swap);
    public ActionModelBuilder WithHxPushUrl(string pushUrl = "true") => WithAttribute("hx-push-url", pushUrl);
    public ActionModelBuilder WithHxInclude(string selector) => WithAttribute("hx-include", selector);

    protected override Task<ActionModel> BuildImpl()
    => Task.FromResult(new ActionModel(_config));
}