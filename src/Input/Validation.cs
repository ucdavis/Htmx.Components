namespace Htmx.Components.Input.Validation;

/// <summary>
/// Defines a validator that can validate string input values.
/// </summary>
/// <remarks>
/// Validators are registered with <see cref="ValidatorRegistry"/> and can be retrieved
/// by their unique identifier for use in input validation scenarios.
/// </remarks>
public interface IValidator
{
    /// <summary>
    /// Gets the unique identifier for this validator.
    /// </summary>
    /// <value>A string that uniquely identifies this validator instance.</value>
    string Id { get; }

    /// <summary>
    /// Validates the specified input value.
    /// </summary>
    /// <param name="value">The value to validate. May be null.</param>
    /// <returns>A <see cref="ValidationResult"/> indicating whether validation succeeded and any error message.</returns>
    ValidationResult Validate(string? value);
}

/// <summary>
/// Represents the result of a validation operation.
/// </summary>
/// <remarks>
/// This class provides factory methods for creating success and failure results,
/// making it easy to return validation outcomes from validator implementations.
/// </remarks>
public class ValidationResult
{
    /// <summary>
    /// Gets or sets a value indicating whether the validation succeeded.
    /// </summary>
    /// <value>true if validation succeeded; otherwise, false.</value>
    public bool IsValid { get; set; }

    /// <summary>
    /// Gets or sets the error message when validation fails.
    /// </summary>
    /// <value>The error message, or null if validation succeeded.</value>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Creates a successful validation result.
    /// </summary>
    /// <returns>A <see cref="ValidationResult"/> indicating success.</returns>
    public static ValidationResult Success() => new() { IsValid = true };

    /// <summary>
    /// Creates a failed validation result with the specified error message.
    /// </summary>
    /// <param name="message">The error message describing why validation failed.</param>
    /// <returns>A <see cref="ValidationResult"/> indicating failure with the provided message.</returns>
    public static ValidationResult Fail(string message) => new() { IsValid = false, ErrorMessage = message };
}

/// <summary>
/// Registry for managing validator instances by their unique identifiers.
/// </summary>
/// <remarks>
/// This registry allows validators to be registered once and retrieved multiple times
/// for use in different validation scenarios throughout the application.
/// </remarks>
/// <example>
/// <code>
/// var registry = new ValidatorRegistry();
/// registry.Register(new EmailValidator());
/// var validator = registry.Get("email");
/// var result = validator?.Validate("user@example.com");
/// </code>
/// </example>

public class ValidatorRegistry
{
    private readonly Dictionary<string, IValidator> _validators = new();

    /// <summary>
    /// Registers a validator with the registry.
    /// </summary>
    /// <param name="validator">The validator to register.</param>
    /// <exception cref="InvalidOperationException">Thrown when a validator with the same ID is already registered.</exception>
    public void Register(IValidator validator)
    {
        if (_validators.ContainsKey(validator.Id))
            throw new InvalidOperationException($"Validator with ID '{validator.Id}' already registered.");
        _validators.Add(validator.Id, validator);
    }

    /// <summary>
    /// Retrieves a validator by its unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the validator to retrieve.</param>
    /// <returns>The validator instance if found; otherwise, null.</returns>
    public IValidator? Get(string id)
    {
        _validators.TryGetValue(id, out var validator);
        return validator;
    }
}

