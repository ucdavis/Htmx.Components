namespace Htmx.Components.Attributes;

/// <summary>
/// Marks methods in controllers that configure model handlers for specific model types.
/// This attribute is used by the <see cref="Configuration.ModelHandlerAttributeRegistrar"/>
/// to automatically discover and register model handlers during application startup.
/// </summary>
/// <remarks>
/// Methods marked with this attribute must accept a single parameter of type
/// <see cref="Models.Builders.ModelHandlerBuilder{T, TKey}"/> and should configure
/// the model handler for CRUD operations and table display.
/// </remarks>
/// <example>
/// <code>
/// [ModelConfig("users")]
/// private void ConfigureUserModel(ModelHandlerBuilder&lt;User, int&gt; builder)
/// {
///     builder.WithKeySelector(u => u.Id)
///            .WithQueryable(() => _context.Users)
///            .WithTable(table => table.AddSelectorColumn(u => u.Name));
/// }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Method)]
public class ModelConfigAttribute : Attribute
{
    /// <summary>
    /// Gets the unique identifier for the model type that this configuration applies to.
    /// </summary>
    /// <value>A string that uniquely identifies the model type within the application.</value>
    public string ModelTypeId { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ModelConfigAttribute"/> class.
    /// </summary>
    /// <param name="modelTypeId">The unique identifier for the model type.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="modelTypeId"/> is null.</exception>
    public ModelConfigAttribute(string modelTypeId) => ModelTypeId = modelTypeId ?? throw new ArgumentNullException(nameof(modelTypeId));
}