using Htmx.Components.Extensions;

namespace Htmx.Components.Models;

/// <summary>
/// Represents a single input field.
/// </summary>
public interface IInputModel
{
    /// <summary>
    /// Gets the property name that this input represents.
    /// </summary>
    string PropName { get; }
    
    /// <summary>
    /// Gets the unique identifier for this input field.
    /// </summary>
    string Id { get; }
    
    /// <summary>
    /// Gets the model handler that manages this input's data operations.
    /// </summary>
    ModelHandler ModelHandler { get; }
    
    /// <summary>
    /// Gets the display label for this input field.
    /// </summary>
    string? Label { get; }
    
    /// <summary>
    /// Gets the placeholder text for this input field.
    /// </summary>
    string? Placeholder { get; }
    
    /// <summary>
    /// Gets the CSS class(es) to apply to this input field.
    /// </summary>
    string? CssClass { get; }
    
    /// <summary>
    /// Gets the type of input control to render.
    /// </summary>
    InputKind Kind { get; }
    
    /// <summary>
    /// Gets the string representation of the input's value.
    /// </summary>
    string Value { get; }
    
    /// <summary>
    /// Gets or sets the object representation of the input's value.
    /// </summary>
    object? ObjectValue { get; set; }
    
    /// <summary>
    /// Gets the HTML attributes to apply to this input field.
    /// </summary>
    Dictionary<string, string> Attributes { get; }
    
    /// <summary>
    /// Gets the options for select/radio/checkbox inputs.
    /// </summary>
    List<KeyValuePair<string, string>>? Options { get; }
}

/// <summary>
/// Represents a strongly-typed input field for a specific property of type TProp on model type T.
/// </summary>
/// <typeparam name="T">The model type that contains the property</typeparam>
/// <typeparam name="TProp">The type of the property being edited</typeparam>
public class InputModel<T, TProp> : IInputModel
{
    internal InputModel(InputModelConfig<T, TProp> config)
    {
        PropName = config.PropName;
        Id = config.Id;
        ModelHandler = config.ModelHandler;
        TypeId = config.TypeId;
        Label = config.Label;
        Placeholder = config.Placeholder;
        CssClass = config.CssClass;
        Kind = config.Kind;
        ObjectValue = config.ObjectValue;
        Attributes = config.Attributes;
        Options = config.Options;
    }

    /// <summary>
    /// Gets or sets the name of the property this input represents.
    /// Used for binding to the specific property on the model.
    /// </summary>
    public string PropName { get; set; } = "";
    
    /// <summary>
    /// Gets or sets the unique identifier for this input control.
    /// Used as the HTML id attribute and for referencing the input in JavaScript.
    /// </summary>
    public string Id { get; set; } = "";
    
    /// <summary>
    /// Gets or sets the model handler that manages operations for the parent model.
    /// Provides access to CRUD operations and metadata for the model type.
    /// </summary>
    public ModelHandler ModelHandler { get; set; } = null!;
    
    /// <summary>
    /// Gets or sets the type identifier for the model this input belongs to.
    /// Used to identify the model type in form submissions and routing.
    /// </summary>
    public string TypeId { get; set; } = typeof(T).Name;
    
    /// <summary>
    /// Gets or sets the display label for this input.
    /// Shown to users as the field name or description.
    /// </summary>
    public string Label { get; set; } = "";
    
    /// <summary>
    /// Gets or sets the placeholder text displayed when the input is empty.
    /// Provides guidance to users about expected input format or content.
    /// </summary>
    public string? Placeholder { get; set; }
    
    /// <summary>
    /// Gets or sets the CSS class names to apply to the input element.
    /// Used for styling and layout purposes.
    /// </summary>
    public string? CssClass { get; set; }
    
    /// <summary>
    /// Gets or sets the type of input control to render.
    /// Determines the HTML input type and behavior (text, number, select, etc.).
    /// </summary>
    public InputKind Kind { get; set; } = InputKind.Text;
    
    /// <summary>
    /// Gets the string representation of the input's value.
    /// Converts the ObjectValue to a string suitable for HTML input elements.
    /// </summary>
    public string Value => ObjectValue?.ConvertToInputString() ?? string.Empty;
    
    /// <summary>
    /// Gets or sets the current value as an object for cases where type conversion is needed.
    /// Used for flexible value handling in dynamic scenarios.
    /// </summary>
    public object? ObjectValue { get; set; } = null;
    
    /// <summary>
    /// Gets additional HTML attributes to apply to the input element.
    /// Allows for custom attributes, data attributes, and other HTML properties.
    /// </summary>
    public Dictionary<string, string> Attributes { get; } = new();
    
    /// <summary>
    /// Gets or sets the options available for selection inputs (radio, select, checkbox lists).
    /// Each key-value pair represents a selectable option where the key is the value and value is the display text.
    /// </summary>
    public List<KeyValuePair<string, string>>? Options { get; set; }

}

/// <summary>
/// Represents a set of input fields.
/// </summary>
public class InputSet
{
    /// <summary>
    /// Initializes a new instance of the InputSet class with the specified configuration.
    /// </summary>
    /// <param name="config">The configuration to use for initializing the input set</param>
    internal InputSet(InputSetConfig config)
    {
        Label = config.Label;
        Inputs = config.Inputs;
    }

    /// <summary>
    /// Initializes a new instance of the InputSet class with default values.
    /// </summary>
    public InputSet() : this(new InputSetConfig()) { }

    /// <summary>
    /// Gets or sets the label for this input set.
    /// Displayed as a group header or section title for the contained inputs.
    /// </summary>
    public string? Label { get; set; } = null;
    
    /// <summary>
    /// Gets or sets the collection of input models contained in this set.
    /// Each input model represents a form field or control.
    /// </summary>
    public List<IInputModel> Inputs { get; set; } = new();
}

/// <summary>
/// Specifies the type of input control to render.
/// Determines the HTML input type and behavior for form fields.
/// </summary>
public enum InputKind
{
    /// <summary>
    /// Standard text input field for single-line text entry.
    /// </summary>
    Text,
    
    /// <summary>
    /// Multi-line text area for longer text content.
    /// </summary>
    TextArea,
    
    /// <summary>
    /// Numeric input field with validation for numbers.
    /// </summary>
    Number,
    
    /// <summary>
    /// Date picker input for date selection.
    /// </summary>
    Date,
    
    /// <summary>
    /// Checkbox input for boolean true/false values.
    /// </summary>
    Checkbox,
    
    /// <summary>
    /// Radio button input for selecting one option from a group.
    /// </summary>
    Radio,
    
    /// <summary>
    /// Dropdown select list for choosing from predefined options.
    /// </summary>
    Select,
    
    /// <summary>
    /// Lookup input with search/autocomplete functionality.
    /// </summary>
    Lookup
}

/// <summary>
/// Internal configuration class used by the framework to store input model settings.
/// This class contains all the configuration options for building input models and 
/// should not be used directly in user code.
/// </summary>
/// <remarks>
/// This class is used internally by <see cref="Builders.InputModelBuilder{T, TProp}"/> to store
/// configuration state during the building process.
/// </remarks>
internal class InputModelConfig<T, TProp>
{
    public string PropName { get; set; } = "";
    public string Id { get; set; } = "";
    public ModelHandler ModelHandler { get; set; } = null!;
    public string TypeId { get; set; } = typeof(T).Name;
    public string Label { get; set; } = "";
    public string? Placeholder { get; set; }
    public string? CssClass { get; set; }
    public InputKind Kind { get; set; } = InputKind.Text;
    public object? ObjectValue { get; set; } = null;
    public Dictionary<string, string> Attributes { get; } = new();
    public List<KeyValuePair<string, string>>? Options { get; set; }
}

internal class InputSetConfig
{
    public string? Label { get; set; }
    public List<IInputModel> Inputs { get; set; } = new();
}