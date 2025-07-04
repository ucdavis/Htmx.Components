using System.Linq.Expressions;
using Htmx.Components.Extensions;
using Htmx.Components.Models;
using Humanizer;

namespace Htmx.Components.Models.Builders;

public interface IInputModelBuilder
{
    Task<IInputModel> Build();
}

public class InputModelBuilder<T, TProp> : BuilderBase<InputModelBuilder<T, TProp>, InputModel<T, TProp>>, IInputModelBuilder
    where T : class
{
    private readonly InputModelConfig<T, TProp> _config = new();

    internal InputModelBuilder(IServiceProvider serviceProvider, Expression<Func<T, TProp>> propertySelector)
        : base(serviceProvider)
    {
        var propName = propertySelector.GetPropertyName();
        _config.PropName = propName;
        _config.Id = propName.SanitizeForHtmlId();
        _config.Kind = GetInputKind(typeof(TProp));
        _config.Label = propName.Humanize(LetterCasing.Title);
    }

    public InputModelBuilder<T, TProp> WithKind(InputKind kind)
    {
        _config.Kind = kind;
        return this;
    }

    public InputModelBuilder<T, TProp> WithName(string name)
    {
        _config.PropName = name;
        return this;
    }

    public InputModelBuilder<T, TProp> WithLabel(string label)
    {
        _config.Label = label;
        return this;
    }

    public InputModelBuilder<T, TProp> WithPlaceholder(string placeholder)
    {
        _config.Placeholder = placeholder;
        return this;
    }

    public InputModelBuilder<T, TProp> WithCssClass(string cssClass)
    {
        _config.CssClass = cssClass;
        return this;
    }

    public InputModelBuilder<T, TProp> WithValue(TProp value)
    {
        _config.ObjectValue = value;
        return this;
    }

    public InputModelBuilder<T, TProp> WithAttribute(string key, string value)
    {
        _config.Attributes[key] = value;
        return this;
    }

    public InputModelBuilder<T, TProp> WithOptions(IEnumerable<KeyValuePair<string, string>> options)
    {
        _config.Options = options.ToList();
        return this;
    }

    protected override Task<InputModel<T, TProp>> BuildImpl()
        => Task.FromResult(new InputModel<T, TProp>(_config));

    async Task<IInputModel> IInputModelBuilder.Build()
    {
        return await base.BuildAsync();
    }

    private static InputKind GetInputKind(Type type)
    {
        var underlyingType = Nullable.GetUnderlyingType(type) ?? type;
        return underlyingType switch
        {
            Type t when t == typeof(string) => InputKind.Text,
            Type t when t == typeof(DateTime) => InputKind.Date,
            Type t when t == typeof(int) || t == typeof(long) || t == typeof(float) || t == typeof(double) => InputKind.Number,
            Type t when t == typeof(bool) => InputKind.Checkbox,
            Type t when t.IsEnum => InputKind.Radio,
            _ => InputKind.Text
        };
    }
}
