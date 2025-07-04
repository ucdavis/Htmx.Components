using System.Linq.Expressions;
using Htmx.Components.Extensions;
using Htmx.Components.Models;
using Humanizer;

namespace Htmx.Components.Models.Builders;

/// <summary>
/// Interface for input model builders that can create IInputModel instances.
/// Provides a common contract for building input models regardless of their specific generic types.
/// </summary>
public interface IInputModelBuilder
{
    /// <summary>
    /// Builds the input model instance asynchronously.
    /// </summary>
    /// <returns>A task that represents the asynchronous build operation, containing the built input model</returns>
    Task<IInputModel> Build();
}

/// <summary>
/// Builder for creating strongly-typed input models for specific properties.
/// Provides fluent configuration for form input controls with type safety and property binding.
/// </summary>
/// <typeparam name="T">The model type that contains the property</typeparam>
/// <typeparam name="TProp">The type of the property being configured</typeparam>
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

    /// <summary>
    /// Sets the input kind (type) for the input control.
    /// Determines how the input will be rendered (text, number, select, etc.).
    /// </summary>
    /// <param name="kind">The input kind to use for rendering</param>
    /// <returns>The current builder instance for method chaining</returns>
    public InputModelBuilder<T, TProp> WithKind(InputKind kind)
    {
        _config.Kind = kind;
        return this;
    }

    /// <summary>
    /// Sets the name attribute for the input control.
    /// Overrides the default name derived from the property name.
    /// </summary>
    /// <param name="name">The name to use for the input control</param>
    /// <returns>The current builder instance for method chaining</returns>
    public InputModelBuilder<T, TProp> WithName(string name)
    {
        _config.PropName = name;
        return this;
    }

    /// <summary>
    /// Sets the display label for the input control.
    /// Overrides the default label derived from the property name.
    /// </summary>
    /// <param name="label">The label text to display</param>
    /// <returns>The current builder instance for method chaining</returns>
    public InputModelBuilder<T, TProp> WithLabel(string label)
    {
        _config.Label = label;
        return this;
    }

    /// <summary>
    /// Sets the placeholder text for the input control.
    /// Provides guidance to users about expected input.
    /// </summary>
    /// <param name="placeholder">The placeholder text to display</param>
    /// <returns>The current builder instance for method chaining</returns>
    public InputModelBuilder<T, TProp> WithPlaceholder(string placeholder)
    {
        _config.Placeholder = placeholder;
        return this;
    }

    /// <summary>
    /// Sets CSS classes to apply to the input control.
    /// Used for styling and layout customization.
    /// </summary>
    /// <param name="cssClass">The CSS class names to apply</param>
    /// <returns>The current builder instance for method chaining</returns>
    public InputModelBuilder<T, TProp> WithCssClass(string cssClass)
    {
        _config.CssClass = cssClass;
        return this;
    }

    /// <summary>
    /// Sets the initial value for the input control.
    /// Provides a default or pre-populated value.
    /// </summary>
    /// <param name="value">The initial value to set</param>
    /// <returns>The current builder instance for method chaining</returns>
    public InputModelBuilder<T, TProp> WithValue(TProp value)
    {
        _config.ObjectValue = value;
        return this;
    }

    /// <summary>
    /// Adds a custom HTML attribute to the input control.
    /// Useful for data attributes, accessibility properties, or custom behavior.
    /// </summary>
    /// <param name="key">The attribute name</param>
    /// <param name="value">The attribute value</param>
    /// <returns>The current builder instance for method chaining</returns>
    public InputModelBuilder<T, TProp> WithAttribute(string key, string value)
    {
        _config.Attributes[key] = value;
        return this;
    }

    /// <summary>
    /// Sets the options for selection-based input controls (select, radio, checkbox).
    /// Each key-value pair represents an option where the key is the value and value is the display text.
    /// </summary>
    /// <param name="options">The collection of options to use</param>
    /// <returns>The current builder instance for method chaining</returns>
    public InputModelBuilder<T, TProp> WithOptions(IEnumerable<KeyValuePair<string, string>> options)
    {
        _config.Options = options.ToList();
        return this;
    }

    /// <summary>
    /// Builds the final InputModel instance with all configured properties.
    /// This method is called internally to create the final result.
    /// </summary>
    /// <returns>A task that represents the asynchronous build operation, containing the built InputModel</returns>
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
